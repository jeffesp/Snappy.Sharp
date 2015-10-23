using System;
using System.Diagnostics;

namespace Snappy.Sharp
{
    public class SnappyCompressor
    {
        const int BLOCK_LOG = 15;
        const int BLOCK_SIZE = 1 << BLOCK_LOG;

        const int INPUT_MARGIN_BYTES = 15;

        const int MAX_HASH_TABLE_BITS = 14;
        const int MAX_HASH_TABLE_SIZE = 1 << MAX_HASH_TABLE_BITS;

        readonly Func<byte[], int, int, int, int> FindMatchLength;

        public SnappyCompressor() : this(Utilities.NativeIntPtrSize())
        {
        }

        internal SnappyCompressor(int intPtrBytes)
        {
            if (intPtrBytes == 4)
            {
                Debug.WriteLine("Using 32-bit optimized FindMatchLength");
                FindMatchLength = FindMatchLength32; 
            }
            else if (intPtrBytes == 8)
            {
                Debug.WriteLine("Using 64-bit optimized FindMatchLength");
                FindMatchLength = FindMatchLength64;
            }
            else
            {
                Debug.WriteLine("Using unoptimized FindMatchLength");
                FindMatchLength = FindMatchLengthBasic;
            }
        }

        public int MaxCompressedLength(int sourceLength)
        {
            return 32 + sourceLength + sourceLength / 6;
        }

        public int Compress(byte[] uncompressed, int uncompressedOffset, int uncompressedLength, byte[] compressed)
        {
            return Compress(uncompressed, uncompressedOffset, uncompressedLength, compressed, 0);
        }

        public int Compress(byte[] uncompressed, int uncompressedOffset, int uncompressedLength, byte[] compressed, int compressedOffset)
        {
            byte[] uncompressedLengthBytes = uncompressedLength.ToVarInt();
            Buffer.BlockCopy(uncompressedLengthBytes, 0, compressed, compressedOffset, uncompressedLengthBytes.Length);
            
            int headLength = uncompressedLengthBytes.Length - compressedOffset; // TODO: this seems really weird - could easily be negative
            return headLength + CompressInternal(uncompressed, uncompressedOffset, uncompressedLength, compressed, compressedOffset + uncompressedLengthBytes.Length);
        }

        internal int CompressInternal(byte[] uncompressed, int uncompressedOffset, int uncompressedLength, byte[] compressed, int compressedOffset)
        {
            // first time through set to offset.
            int compressedIndex = compressedOffset;
            short[] hashTable = GetHashTable(uncompressedLength);

            for (int read = 0; read < uncompressedLength; read += BLOCK_SIZE)
            {
                // Get encoding table for compression
                Array.Clear(hashTable, 0, hashTable.Length);

                compressedIndex = CompressFragment(
                    uncompressed,
                    uncompressedOffset + read,
                    Math.Min(uncompressedLength - read, BLOCK_SIZE),
                    compressed,
                    compressedIndex,
                    hashTable);
            }
            return compressedIndex - compressedOffset;

        }

        internal int CompressFragment(byte[] input, int inputOffset, int inputSize, byte[] output, int outputIndex, short[] hashTable)
        {
            // "ip" is the input pointer, and "op" is the output pointer.
            int inputIndex = inputOffset;
            Debug.Assert(inputSize <= BLOCK_SIZE);
            Debug.Assert((hashTable.Length & (hashTable.Length - 1)) == 0, "hashTable size must be a power of 2");
            int shift = (int) (32 - Utilities.Log2Floor((uint)hashTable.Length));
            int inputEnd = inputOffset + inputSize;
            int baseInputIndex = inputIndex;
            int nextEmitIndex = inputIndex;

            if (inputSize >= INPUT_MARGIN_BYTES)
            {
                int ipLimit = inputOffset + inputSize - INPUT_MARGIN_BYTES;

                uint currentIndexBytes = Utilities.GetFourBytes(input, ++inputIndex);
                for (uint nextHash = Hash(currentIndexBytes, shift); ; )
                {
                    Debug.Assert(nextEmitIndex < inputIndex);
                    // The body of this loop calls EmitLiteral once and then EmitCopy one or
                    // more times.  (The exception is that when we're close to exhausting
                    // the input we goto emit_remainder.)
                    //
                    // In the first iteration of this loop we're just starting, so
                    // there's nothing to copy, so calling EmitLiteral once is
                    // necessary.  And we only start a new iteration when the
                    // current iteration has determined that a call to EmitLiteral will
                    // precede the next call to EmitCopy (if any).
                    //
                    // Step 1: Scan forward in the input looking for a 4-byte-long match.
                    // If we get close to exhausting the input then goto emit_remainder.
                    //
                    // Heuristic match skipping: If 32 bytes are scanned with no matches
                    // found, start looking only at every other byte. If 32 more bytes are
                    // scanned, look at every third byte, etc.. When a match is found,
                    // immediately go back to looking at every byte. This is a small loss
                    // (~5% performance, ~0.1% density) for compressible data due to more
                    // bookkeeping, but for non-compressible data (such as JPEG) it's a huge
                    // win since the compressor quickly "realizes" the data is incompressible
                    // and doesn't bother looking for matches everywhere.
                    //
                    // The "skip" variable keeps track of how many bytes there are since the
                    // last match; dividing it by 32 (ie. right-shifting by five) gives the
                    // number of bytes to move ahead for each iteration.
                    uint skip = 32;

                    int nextIp = inputIndex;
                    int candidate;
                    do
                    {
                        inputIndex = nextIp;
                        uint hash = nextHash;
                        Debug.Assert(hash == Hash(Utilities.GetFourBytes(input, inputIndex), shift));
                        nextIp = (int)(inputIndex + (skip++ >> 5));
                        if (nextIp > ipLimit)
                        {
                            goto emit_remainder;
                        }
                        currentIndexBytes = Utilities.GetFourBytes(input, nextIp);
                        nextHash = Hash(currentIndexBytes, shift);
                        candidate = baseInputIndex + hashTable[hash];
                        Debug.Assert(candidate >= baseInputIndex);
                        Debug.Assert(candidate < inputIndex);

                        hashTable[hash] = (short)(inputIndex - baseInputIndex);
                    } while (Utilities.GetFourBytes(input, inputIndex) != Utilities.GetFourBytes(input, candidate));

                    // Step 2: A 4-byte match has been found.  We'll later see if more
                    // than 4 bytes match.  But, prior to the match, input
                    // bytes [next_emit, ip) are unmatched.  Emit them as "literal bytes."
                    Debug.Assert(nextEmitIndex + 16 < inputEnd);
                    outputIndex = EmitLiteral(output, outputIndex, input, nextEmitIndex, inputIndex - nextEmitIndex);

                    // Step 3: Call EmitCopy, and then see if another EmitCopy could
                    // be our next move.  Repeat until we find no match for the
                    // input immediately after what was consumed by the last EmitCopy call.
                    //
                    // If we exit this loop normally then we need to call EmitLiteral next,
                    // though we don't yet know how big the literal will be.  We handle that
                    // by proceeding to the next iteration of the main loop.  We also can exit
                    // this loop via goto if we get close to exhausting the input.
                    uint candidateBytes = 0;
                    int insertTail;

                    do
                    {
                        // We have a 4-byte match at ip, and no need to emit any
                        // "literal bytes" prior to ip.
                        int baseIndex = inputIndex;
                        int matched = 4 + FindMatchLength(input, candidate + 4, inputIndex + 4, inputEnd);
                        inputIndex += matched;
                        int offset = baseIndex - candidate;
                        //DCHECK_EQ(0, memcmp(baseIndex, candidate, matched));
                        outputIndex = EmitCopy(output, outputIndex, offset, matched);
                        // We could immediately start working at ip now, but to improve
                        // compression we first update table[Hash(ip - 1, ...)].
                        insertTail = inputIndex - 1;
                        nextEmitIndex = inputIndex;
                        if (inputIndex >= ipLimit)
                        {
                            goto emit_remainder;
                        }
                        uint prevHash = Hash(Utilities.GetFourBytes(input, insertTail), shift);
                        hashTable[prevHash] = (short)(inputIndex - baseInputIndex - 1);
                        uint curHash = Hash(Utilities.GetFourBytes(input, insertTail + 1), shift);
                        candidate = baseInputIndex + hashTable[curHash];
                        candidateBytes = Utilities.GetFourBytes(input, candidate);
                        hashTable[curHash] = (short)(inputIndex - baseInputIndex);
                    } while (Utilities.GetFourBytes(input, insertTail + 1) == candidateBytes);

                    nextHash = Hash(Utilities.GetFourBytes(input, insertTail + 2), shift);
                    ++inputIndex;
                }
            }

        emit_remainder:
            // Emit the remaining bytes as a literal
            if (nextEmitIndex < inputEnd)
            {
                outputIndex = EmitLiteral(output, outputIndex, input, nextEmitIndex, inputEnd - nextEmitIndex);
            }

            return outputIndex;
        }

        private int EmitCopyLessThan64(byte[] output, int outputIndex, int offset, int length)
        {
            Debug.Assert( offset >= 0);
            Debug.Assert( length <= 64);
            Debug.Assert( length >= 4);
            Debug.Assert( offset < 65536);

            if ((length < 12) && (offset < 2048)) {
                int lenMinus4 = length - 4;
                Debug.Assert(lenMinus4 < 8);            // Must fit in 3 bits
                output[outputIndex++] = (byte) (Snappy.COPY_1_BYTE_OFFSET | ((lenMinus4) << 2) | ((offset >> 8) << 5));
                output[outputIndex++] = (byte) (offset);
            }
            else {
                output[outputIndex++] = (byte) (Snappy.COPY_2_BYTE_OFFSET | ((length - 1) << 2));
                output[outputIndex++] = (byte) (offset);
                output[outputIndex++] = (byte) (offset >> 8);
            }
            return outputIndex;
        }

        private int EmitCopy(byte[] compressed, int compressedIndex, int offset, int length)
        {
            // Emit 64 byte copies but make sure to keep at least four bytes reserved
            while (length >= 68)
            {
                compressedIndex = EmitCopyLessThan64(compressed, compressedIndex, offset, 64);
                length -= 64;
            }

            // Emit an extra 60 byte copy if have too much data to fit in one copy
            if (length > 64)
            {
                compressedIndex = EmitCopyLessThan64(compressed, compressedIndex, offset, 60);
                length -= 60;
            }

            // Emit remainder
            compressedIndex = EmitCopyLessThan64(compressed, compressedIndex, offset, length);
            return compressedIndex;
        }

        // Return the largest n such that
        //
        //   source[startIndex,n-1] == source[matchIndex,n-1]
        //   and n <= (matchIndexLimit - matchIndex).
        //
        // Does not read matchIndexLimit or beyond.
        // Does not read *(source + (matchIndexLimit - matchIndex)) or beyond.
        // Requires that matchIndexLimit >= matchIndex.
        private int FindMatchLengthBasic(byte[] source, int startIndex, int matchIndex, int matchIndexLimit)
        {
            Debug.Assert(matchIndexLimit >= matchIndex);
            int matched = 0;
            while (matchIndex + matched < matchIndexLimit && source[startIndex + matched] == source[matchIndex + matched]) {
                ++matched;
            }
            return matched;
        }

        // 32-bit optimized version of above
        private int FindMatchLength32(byte[] source, int startIndex, int matchIndex, int matchIndexLimit)
        {
            Debug.Assert(matchIndexLimit >= matchIndex);

            int matched = 0;
            while (matchIndex <= matchIndexLimit - 4)
            {
                uint a = Utilities.GetFourBytes(source, matchIndex);
                uint b = Utilities.GetFourBytes(source, startIndex + matched);

                if (a == b)
                {
                    matchIndex += 4;
                    matched += 4;
                }
                else
                {
                    uint c = a ^ b;
                    int matchingBits = (int)Utilities.NumberOfTrailingZeros(c);
                    matched += matchingBits >> 3;
                    return matched;
                }
            }
            while (matchIndex < matchIndexLimit)
            {
                if (source[startIndex] == source[matchIndex])
                {
                    ++matchIndex;
                    ++matched;
                }
                else
                {
                    return matched;
                }
            }
            return matched;
        }


        // 64-bit optimized version of above
        int FindMatchLength64(byte[] source, int startIndex, int matchIndex, int matchIndexLimit)
        {
            Debug.Assert(matchIndexLimit >= matchIndex);

            int matched = 0;
            while (matchIndex <= matchIndexLimit - 8)
            {
                ulong a = Utilities.GetEightBytes(source, matchIndex);
                ulong b = Utilities.GetEightBytes(source, startIndex + matched);

                if (a == b)
                {
                    matchIndex += 8;
                    matched += 8;
                }
                else
                {
                    ulong c = a ^ b;
                    // first get low order 32 bits, if all 0 then get high order as well.
                    int matchingBits = (int)Utilities.NumberOfTrailingZeros(c);
                    matched += matchingBits >> 3;
                    return matched;
                }
            }
            while (matchIndex < matchIndexLimit)
            {
                if (source[startIndex] == source[matchIndex])
                {
                    ++matchIndex;
                    ++matched;
                }
                else
                {
                    return matched;
                }
            }
            return matched;
        }


        internal int EmitLiteral(byte[] output, int outputIndex, byte[] literal, int literalIndex, int length)
        {
            int n = length - 1;
            outputIndex = EmitLiteralTagBytes(output, outputIndex, n);
            Buffer.BlockCopy(literal, literalIndex, output, outputIndex, length);
            return outputIndex + length;
        }

        internal int EmitLiteralTagBytes(byte[] output, int outputIndex, int size)
        {
            int bytesUsed = 0;
            if (size < 60)
            {
                output[outputIndex] = (byte)(Snappy.LITERAL | (size << 2));
                bytesUsed = 1;
            }
            else if (size < 1<<8)
            {
                output[outputIndex] = (Snappy.LITERAL | (60 << 2));
                output[outputIndex + 1] = (byte)size;
                bytesUsed = 2;
            }
            else if (size  < 1<<16)
            {
                output[outputIndex] = (Snappy.LITERAL | (61 << 2));
                output[outputIndex + 1] = (byte)size;
                output[outputIndex + 2] = (byte)(size >> 8);
                bytesUsed = 3;
            }
            else if (size  < 1<<24)
            {
                output[outputIndex] = (Snappy.LITERAL | (62 << 2));
                output[outputIndex + 1] = (byte)size;
                output[outputIndex + 2] = (byte)(size >> 8);
                output[outputIndex + 3] = (byte)(size >> 16);
                bytesUsed = 4;
            }
            else if (size  <= int.MaxValue) 
            {
                output[outputIndex] = (Snappy.LITERAL | (63 << 2));
                output[outputIndex + 1] = (byte)size;
                output[outputIndex + 2] = (byte)(size >> 8);
                output[outputIndex + 3] = (byte)(size >> 16);
                output[outputIndex + 4] = (byte)(size >> 24);
                bytesUsed = 5;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Source size is too long.");
            }
            return outputIndex + bytesUsed;
        }

        internal int GetHashTableSize(int inputSize)
        {
            // Use smaller hash table when input.size() is smaller, since we
            // fill the table, incurring O(hash table size) overhead for
            // compression, and if the input is short, we won't need that
            // many hash table entries anyway.
            Debug.Assert(MAX_HASH_TABLE_SIZE >= 256);

            int hashTableSize = 256;
            // TODO: again, java version unrolled, but this time with note that it isn't faster
            while (hashTableSize < MAX_HASH_TABLE_SIZE && hashTableSize < inputSize)
            {
                hashTableSize <<= 1;
            }
            Debug.Assert(0 == (hashTableSize & (hashTableSize - 1)), "hash must be power of two");
            Debug.Assert(hashTableSize <= MAX_HASH_TABLE_SIZE, "hash table too large");
            return hashTableSize;
        }

        uint Hash(uint bytes, int shift)
        {
            const int kMul = 0x1e35a7bd;
            return (bytes * kMul) >> shift;
        }

        internal int WriteUncomressedLength(byte[] compressed, int compressedOffset, int uncompressedLength)
        {
            const int bitMask = 0x80;
            if (uncompressedLength < 0)
                throw new ArgumentException("uncompressedLength");

            // A little-endian varint. 
            // From doc:
            // Varints consist of a series of bytes, where the lower 7 bits are data and the upper bit is set iff there are more bytes to read.
            // In other words, an uncompressed length of 64 would be stored as 0x40, and an uncompressed length of 2097150 (0x1FFFFE) would
            // be stored as 0xFE 0XFF 0X7F
            while (uncompressedLength > bitMask) 
            {
                compressed[compressedOffset++] = (byte)(uncompressedLength | bitMask);
                uncompressedLength = uncompressedLength >> 7;
            }
            compressed[compressedOffset++] = (byte)(uncompressedLength);

            return compressedOffset;
        }

        internal short[] GetHashTable(int inputLength)
        {
            // Use smaller hash table when input.size() is smaller, since we
            // fill the table, incurring O(hash table size) overhead for
            // compression, and if the input is short, we won't need that
            // many hash table entries anyway.
            Debug.Assert(MAX_HASH_TABLE_SIZE > 256);
            int tableSize = 256;
            while (tableSize < MAX_HASH_TABLE_SIZE && tableSize < inputLength)
            {
                tableSize <<= 1;
            }
            Debug.Assert((tableSize & (tableSize - 1)) == 0, "Table size not power of 2.");
            Debug.Assert(tableSize <= MAX_HASH_TABLE_SIZE, "Table size too large.");
            return new short[tableSize];
        }
    }
}
