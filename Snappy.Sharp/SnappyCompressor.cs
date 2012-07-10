using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snappy.Sharp
{
    public class SnappyCompressor
    {
        public SnappyCompressor() 
        {
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
            return uncompressedLength;
        }

        public int WriteUncomressedLength(byte[] compressed, int compressedOffset, int uncompressedLength)
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
    }
}
