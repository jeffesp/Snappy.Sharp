using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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

        public SnappyCompressor() 
        {
        }

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

            for (int read = 0; read < uncompressedLength; read += BLOCK_SIZE) {
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
            throw new NotImplementedException();
        }

        internal int EmitLiteral(byte[] output, int outputIndex, byte[] literal, int literalIndex, int length, bool allowFastPath)
        {
            int n = length - 1;
            if (n < 60)
            {
                // Size fits in tag byte.
                output[outputIndex++] = (byte) (LITERAL | (n << 2));
                if (allowFastPath && n <= 16)
                {
                    Utilities.UnalignedCopy64(literal, literalIndex, output, outputIndex );
                    Utilities.UnalignedCopy64(literal, literalIndex + 8, output, outputIndex + 8);
                    return outputIndex + length;
                }
            }
            else
            {
                // setup a spot to hold the tag byte.
                int baseIndex = outputIndex;
                outputIndex++;
                // TODO: Java version is 'unrolled' here, C++ isn't. Should look into it?
                int count = 0;
                while (n > 0) {
                  output[outputIndex++] = (byte)(n & 0xff);
                  n >>= 8;
                  count++;
                }
                Debug.Assert(count >= 1);
                Debug.Assert(count <= 4);
                output[baseIndex] = (byte) (LITERAL | ((59+count) << 2));
            }
            Buffer.BlockCopy(literal, literalIndex, output, outputIndex, length);
            return outputIndex + length;
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
            // TODO: C++/Java versions do this with a reusable buffer for efficiency. Probably also useful here. All that not allocating in a tight loop and all
            return new short[tableSize];
        }
    }
}
