using System;
using System.Diagnostics;

namespace Snappy.Sharp
{
    public class SnappyCompressor
    {
        private const int BLOCK_LOG = 15;
        private const int BLOCK_SIZE = 1 << BLOCK_LOG;

        private const int INPUT_MARGIN_BYTES = 15;

        private const int MAX_HASH_TABLE_BITS = 14;
        private const int MAX_HASH_TABLE_SIZE = 1 << MAX_HASH_TABLE_BITS;
        private const int LITERAL = 0;
        private const int COPY_1_BYTE_OFFSET = 1;  // 3 bit length + 3 bits of offset in opcode
        private const int COPY_2_BYTE_OFFSET = 2;
        private const int COPY_4_BYTE_OFFSET = 3;

        public int MaxCompressedLength(int sourceLength)
        {
            // So says the code from Google.
            return 32 + sourceLength + sourceLength / 6;
        }

        public int Compress(byte[] uncompressed, int uncompressedOffset, int uncompressedLength, byte[] compressed)
        {
            return Compress(uncompressed, uncompressedOffset, uncompressedLength, compressed, 0);
        }

        public int Compress(byte[] uncompressed, int uncompressedOffset, int uncompressedLength, byte[] compressed, int compressedOffset)
        {
            int compressedIndex = WriteUncomressedLength(compressed, compressedOffset, uncompressedLength);
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

        internal int CompressFragment(byte[] uncompressed, int uncompressedOffset, int uncompressedLength, byte[] compressed, int compressedIndex, short[] hashTable)
        {
            int ipIndex = uncompressedOffset;
            Debug.Assert(uncompressedLength <= BLOCK_SIZE);
            int ipEndIndex = uncompressedOffset + uncompressedLength;

            int hashTableSize = GetHashTableSize(uncompressedLength);
            int shift = 32 - Utilities.Log2Floor(hashTableSize);
            Debug.Assert((hashTableSize & (hashTableSize - 1)) == 0, "table must be power of two");
            Debug.Assert(0xFFFFFFFF >> shift == hashTableSize - 1);

            // Bytes in [nextEmitIndex, ipIndex) will be emitted as literal bytes.  Or
            // [nextEmitIndex, ipEndIndex) after the main loop.
            int nextEmitIndex = ipIndex;

            if (uncompressedLength >= INPUT_MARGIN_BYTES)
            {
                int ipLimit = uncompressedOffset + uncompressedLength - INPUT_MARGIN_BYTES;
                while (ipIndex <= ipLimit)
                {
                    Debug.Assert(nextEmitIndex <= ipIndex);

                    // The body of this loop calls EmitLiteral once and then EmitCopy one or
                    // more times.  (The exception is that when we're close to exhausting
                    // the input we exit and emit a literal.)
                    //
                    // In the first iteration of this loop we're just starting, so
                    // there's nothing to copy, so calling EmitLiteral once is
                    // necessary.  And we only start a new iteration when the
                    // current iteration has determined that a call to EmitLiteral will
                    // precede the next call to EmitCopy (if any).
                    //
                    // Step 1: Scan forward in the input looking for a 4-byte-long match.
                    // If we get close to exhausting the input exit and emit a final literal.
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
                    int skip = 32;

                    int[] candidateResult = FindCandidate(uncompressed, ipIndex, ipLimit, uncompressedOffset, shift, hashTable, skip);
                    ipIndex = candidateResult[0];
                    skip = candidateResult[1];
                    int candidateIndex = candidateResult[2];
                    if (ipIndex + BytesBetweenHashLookups(skip) > ipLimit)
                    {
                        break;
                    }

                    // Step 2: A 4-byte match has been found.  We'll later see if more
                    // than 4 bytes match.  But, prior to the match, input
                    // bytes [nextEmit, ip) are unmatched.  Emit them as "literal bytes."
                    Debug.Assert(nextEmitIndex + 16 <= ipEndIndex);
                    compressedIndex = EmitLiteral(compressed, compressedIndex, uncompressed, nextEmitIndex, ipIndex - nextEmitIndex, true);

                    // Step 3: Call EmitCopy, and then see if another EmitCopy could
                    // be our next move.  Repeat until we find no match for the
                    // input immediately after what was consumed by the last EmitCopy call.
                    //
                    // If we exit this loop normally then we need to call EmitLiteral next,
                    // though we don't yet know how big the literal will be.  We handle that
                    // by proceeding to the next iteration of the main loop.  We also can exit
                    // this loop via goto if we get close to exhausting the input.
                    int[] indexes = EmitCopies(uncompressed, uncompressedOffset, uncompressedLength, ipIndex, compressed, compressedIndex, hashTable, shift, candidateIndex);
                    ipIndex = indexes[0];
                    compressedIndex = indexes[1];
                    nextEmitIndex = ipIndex;
                }
            }

            // goto emitRemainder hack
            if (nextEmitIndex < ipEndIndex)
            {
                // Emit the remaining bytes as a literal
                compressedIndex = EmitLiteral(compressed, compressedIndex, uncompressed, nextEmitIndex, ipEndIndex - nextEmitIndex, false);
            }
            return compressedIndex;
        }

        private int[] EmitCopies(byte[] uncompressed, int uncompressedOffset, int uncompressedLength, int ipIndex, byte[] compressed, int compressedIndex, short[] hashTable, int shift, int candidateIndex)
        {
            // Step 3: Call EmitCopy, and then see if another EmitCopy could
            // be our next move.  Repeat until we find no match for the
            // input immediately after what was consumed by the last EmitCopy call.
            //
            // If we exit this loop normally then we need to call EmitLiteral next,
            // though we don't yet know how big the literal will be.  We handle that
            // by proceeding to the next iteration of the main loop.  We also can exit
            // this loop via goto if we get close to exhausting the input.
            uint inputBytes;
            do {
                // We have a 4-byte match at ip, and no need to emit any
                // "literal bytes" prior to ip.
                int matched = 4 + FindMatchLength(uncompressed, candidateIndex + 4, uncompressed, ipIndex + 4, uncompressedOffset + uncompressedLength);
                int offset = ipIndex - candidateIndex;
                //TODO: assert SnappyInternalUtils.equals(input, ipIndex, input, candidateIndex, matched);
                ipIndex += matched;

                // emit the copy operation for this chunk
                compressedIndex = EmitCopy(compressed, compressedIndex, offset, matched);

                // are we done?
                if (ipIndex >= uncompressedOffset + uncompressedLength - INPUT_MARGIN_BYTES) {
                    return new int[]{ipIndex, compressedIndex};
                }

                // We could immediately start working at ip now, but to improve
                // compression we first update table[Hash(ip - 1, ...)].
                ulong temp = Utilities.GetULong(uncompressed, ipIndex - 1);
                uint prevInt = (uint) temp;
                inputBytes = (uint)(temp >> 8);

                // add hash starting with previous byte
                uint prevHash = HashBytes(prevInt, shift);
                hashTable[prevHash] = (short) (ipIndex - uncompressedOffset - 1);

                // update hash of current byte
                uint curHash = HashBytes(inputBytes, shift);

                candidateIndex = uncompressedOffset + hashTable[curHash];
                hashTable[curHash] = (short) (ipIndex - uncompressedOffset);

            } while (inputBytes == Utilities.GetUInt(uncompressed, candidateIndex));
            return new int[]{ipIndex, compressedIndex};
        }

        private static int EmitCopyLessThan64(byte[] output, int outputIndex, int offset, int length)
        {
            Debug.Assert( offset >= 0);
            Debug.Assert( length <= 64);
            Debug.Assert( length >= 4);
            Debug.Assert( offset < 65536);

            if ((length < 12) && (offset < 2048)) {
                int lenMinus4 = length - 4;
                Debug.Assert(lenMinus4 < 8);            // Must fit in 3 bits
                output[outputIndex++] = (byte) (COPY_1_BYTE_OFFSET | ((lenMinus4) << 2) | ((offset >> 8) << 5));
                output[outputIndex++] = (byte) (offset);
            }
            else {
                output[outputIndex++] = (byte) (COPY_2_BYTE_OFFSET | ((length - 1) << 2));
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
        //   s1[0,n-1] == s2[0,n-1]
        //   and n <= (s2_limit - s2).
        //
        // Does not read *s2_limit or beyond.
        // Does not read *(s1 + (s2_limit - s2)) or beyond.
        // Requires that s2_limit >= s2.
        private int FindMatchLength(byte[] s1, int s1Index, byte[] s2, int s2Index, int s2Limit)
        {
            Debug.Assert(s2Limit >= s2Index);
            int matched = 0;
            while (s2Index + matched < s2Limit && s1[s1Index + matched] == s2[s2Index + matched]) {
                ++matched;
            }
            return matched;
            //TODO: efficient method of loading more than one byte at a time. make sure to do check if 64bit process and load longs, ints otherwise.
#if false
            int matched = 0;

            while (s1Index + matched < s1.Length && s2Index + matched <= s2Limit - 4 && Utilities.GetUInt(s2, s2Index + matched) == Utilities.GetUInt(s1, s1Index + matched)) {
                matched += 4;
            }

            if (BitConverter.IsLittleEndian && s2Index + matched <= s2Limit - 4 && s1Index + matched < s1.Length) {
                int x = (int)(Utilities.GetUInt(s2, s2Index + matched) ^ Utilities.GetUInt(s1, s1Index + matched));
                int matchingBits = Utilities.NumberOfTrailingZeros(x);
                matched += matchingBits >> 3;
            }
            else {
                while (s2Index + matched < s2Limit && s1[s1Index + matched] == s2[s2Index + matched]) {
                    ++matched;
                }
            }
            return matched;
#endif
        }

        internal int EmitLiteral(byte[] output, int outputIndex, byte[] literal, int literalIndex, int length, bool allowFastPath)
        {
            int n = length - 1;
            outputIndex = EmitLiteralTag(output, outputIndex, n);
            if (allowFastPath && length <= 16)
            {
                Utilities.UnalignedCopy64(literal, literalIndex, output, outputIndex);
                Utilities.UnalignedCopy64(literal, literalIndex + 8, output, outputIndex + 8);
                return outputIndex + length;
            }
            Buffer.BlockCopy(literal, literalIndex, output, outputIndex, length);
            return outputIndex + length;
        }

        internal int EmitLiteralTag(byte[] output, int outputIndex, int size)
        {
            if (size < 60)
            {
                output[outputIndex++] = (byte)(LITERAL | (size << 2));
            }
            else
            {
                int baseIndex = outputIndex;
                outputIndex++;
                // TODO: Java version is 'unrolled' here, C++ isn't. Should look into it?
                int count = 0;
                while (size > 0)
                {
                    output[outputIndex++] = (byte)(size & 0xff);
                    size >>= 8;
                    count++;
                }
                Debug.Assert(count >= 1);
                Debug.Assert(count <= 4);
                output[baseIndex] = (byte)(LITERAL | ((59 + count) << 2));
            }
            return outputIndex;
        }

        internal int[] FindCandidate(byte[] input, int ipIndex, int ipLimit, int inputOffset, int shift, short[] table, int skip)
        {

            int candidateIndex = 0;
            for (ipIndex += 1; ipIndex + BytesBetweenHashLookups(skip) <= ipLimit; ipIndex += BytesBetweenHashLookups(skip++))
            {
                // hash the 4 bytes starting at the input pointer
                uint currentInt = Utilities.GetUInt(input, ipIndex);
                uint hash = HashBytes(currentInt, shift);

                // get the position of a 4 bytes sequence with the same hash
                candidateIndex = inputOffset + table[hash];
                Debug.Assert(candidateIndex >= 0);
                Debug.Assert(candidateIndex < ipIndex);

                // update the hash to point to the current position
                table[hash] = (short)(ipIndex - inputOffset);

                // if the 4 byte sequence a the candidate index matches the sequence at the
                // current position, proceed to the next phase
                if (currentInt == Utilities.GetUInt(input, candidateIndex))
                {
                    break;
                }
            }
            return new int[] { ipIndex, candidateIndex, skip };
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

        private int BytesBetweenHashLookups(int skip)
        {
            return (skip >> 5);
        }

        private uint HashBytes(uint bytes, int shift)
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

            while (uncompressedLength > bitMask) // TODO: java version 'unrolled'. Look at perf characteristics
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
            // TODO: C++/Java versions do this with a reusable buffer for efficiency. Probably also useful here. All that not allocating in a tight loop and all
            return new short[tableSize];
        }
    }
}
