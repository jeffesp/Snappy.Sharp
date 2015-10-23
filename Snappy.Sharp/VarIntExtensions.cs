using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snappy.Sharp
{
    static class VarIntExtensions
    {
        const int bitMask = 0x80;

        public static byte[] ToVarInt(this byte value)
        {
            return VarIntImpl(value);
        }

        public static byte[] ToVarInt(this short value)
        {
            return VarIntImpl(value);
        }

        public static byte[] ToVarInt(this int value)
        {
            return VarIntImpl(value);
        }

        public static byte[] ToVarInt(this long value)
        {
            return VarIntImpl(value);
        }

        static byte[] VarIntImpl(long value)
        {
            int compressedLength = 0;
            byte[] compressed = new byte[10]; // max length for `long` value
            // A little-endian varint. 
            // From doc:
            // Varints consist of a series of bytes, where the lower 7 bits are data and the upper bit is set iff there are more bytes to read.
            // In other words, an uncompressed length of 64 would be stored as 0x40, and an uncompressed length of 2097150 (0x1FFFFE) would
            // be stored as 0xFE 0XFF 0X7F
            while (value > bitMask)
            {
                compressed[compressedLength++] = (byte)(value | bitMask);
                value = value >> 7;
            }
            compressed[compressedLength++] = (byte)(value);

            return compressed.Take(compressedLength).ToArray();
        }

        public static int FromVarInt(this byte[] data)
        {
            int size = 0;
            return FromVarInt(data, ref size);
        }

        public static int FromVarInt(this byte[] data, ref int offset)
        {
            if (data.Length == 0 || offset >= data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            int sum = 0, currentShift = 0;
            while ((data[offset] & bitMask) != 0)
            {
                sum = UpdateSum(data, offset, currentShift, sum);
                offset++;
                currentShift += 7;
            }
            sum = UpdateSum(data, offset, currentShift, sum);
            offset++;
            return sum;
        }

        static int UpdateSum(byte[] data, int offset, int currentShift, int sum)
        {
            int nextValue = data[offset] & (bitMask - 1);
            nextValue <<= currentShift;
            sum += nextValue;
            return sum;
        }

    }
}
