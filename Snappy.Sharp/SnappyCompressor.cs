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

        public virtual int Compress(byte[] uncompressed, int uncompressedOffset, int uncompressedLength, byte[] compressed, int compressedOffset)
        {
            return 0;
        }

        public virtual int WriteUncomressedLength(byte[] compressed, int compressedOffset, int uncompressedLength)
        {
            int bitMask = 0x80;
            if (uncompressedLength < 0)
                throw new ArgumentException("uncompressedLength");

            // A little-endian varint. 
            // From doc:
            // Varints consist of a series of bytes, where the lower 7 bits are data and the upper bit is set iff there are more bytes to read.
            // In other words, an uncompressed length of 64 would be stored as 0x40, and an uncompressed length of 2097150 (0x1FFFFE) would
            // be stored as 0xFE 0XFF 0X7F

            if (uncompressedLength < (1 << 7))
            {
                compressed[compressedOffset++] = (byte)uncompressedLength;
            }
            else if (uncompressedLength < (1 << 14))
            {
                compressed[compressedOffset++] = (byte)(uncompressedLength | bitMask);
                compressed[compressedOffset++] = (byte)(uncompressedLength >> 7);
            }
            else if (uncompressedLength < (1 << 21))
            {
                compressed[compressedOffset++] = (byte)(uncompressedLength | bitMask);
                compressed[compressedOffset++] = (byte)((uncompressedLength >> 7) | bitMask);
                compressed[compressedOffset++] = (byte)((uncompressedLength >> 14));
            }
            else if (uncompressedLength < (1 << 28))
            {
                compressed[compressedOffset++] = (byte)(uncompressedLength | bitMask);
                compressed[compressedOffset++] = (byte)((uncompressedLength >> 7) | bitMask);
                compressed[compressedOffset++] = (byte)((uncompressedLength >> 14) | bitMask);
                compressed[compressedOffset++] = (byte)((uncompressedLength >> 21));
            }
            else 
            {
                compressed[compressedOffset++] = (byte)(uncompressedLength | bitMask);
                compressed[compressedOffset++] = (byte)((uncompressedLength >> 7) | bitMask);
                compressed[compressedOffset++] = (byte)((uncompressedLength >> 14) | bitMask);
                compressed[compressedOffset++] = (byte)((uncompressedLength >> 21) | bitMask);
                compressed[compressedOffset++] = (byte)((uncompressedLength >> 28));
            }
            return compressedOffset;
        }
    }
}
