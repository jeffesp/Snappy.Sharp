using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snappy.Sharp
{
    public class SnappyDecompressor
    {
        private const int bitMask = 0x80;
        public int ReadUncompressedLength(byte[] data, int offset)
        {
            int sum = 0, currentShift = 0;
            while ((data[offset] & bitMask) != 0)
            {
                sum = UpdateSum(data, offset, currentShift, sum);
                offset++;
                currentShift += 7;
            }
            sum = UpdateSum(data, offset, currentShift, sum);
            return sum;
        }

        private static int UpdateSum(byte[] data, int offset, int currentShift, int sum)
        {
            int nextValue;
            nextValue = data[offset] & (bitMask - 1);
            nextValue <<= currentShift;
            sum += nextValue;
            return sum;
        }

        public byte[] Decompress(byte[] compressed, int compressedOffset, int compressedSize)
        {
            var uncompressedSize = ReadUncompressedLength(compressed, 0);
            var data = new byte[uncompressedSize];

            Decompress(compressed, compressedOffset, compressedSize, data, uncompressedSize);

            return data;
        }

        public int Decompress(byte[] compressed, int compressedOffset, int compressedSize, byte[] uncompressed, int uncompressedOffset)
        {
            return 0;
        }
    }
}
