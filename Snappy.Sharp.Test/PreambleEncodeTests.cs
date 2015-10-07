using System;

using Xunit;

namespace Snappy.Sharp.Test
{
    public class PreambleEncodeTests
    {
        [Fact]
        public void size_less_than_zero_throws()
        {
            Assert.Throws<ArgumentException>(() => new SnappyCompressor().WriteUncomressedLength(null, 0, -1));
        }

        [Fact]
        public void one_byte_size()
        {
            var data = WriteLengthData(64);
            Assert.Equal(data[0], 0x40);
        }

        [Fact]
        public void one_byte_size_has_zero_in_next_position()
        {
            var data = WriteLengthData(10);
            Assert.Equal(data[1], 0);
        }

        [Fact]
        public void multi_byte_size()
        {
            var data = WriteLengthData(2097150);
            Assert.Equal(0xFE, data[0]);
            Assert.Equal(0xFF, data[1]);
            Assert.Equal(0x7F, data[2]);
        }

        [Fact]
        public void int_maxvalue_encoded()
        {
            var data = WriteLengthData(int.MaxValue);
            Assert.Equal(0xFF, data[0]);
            Assert.Equal(0xFF, data[1]);
            Assert.Equal(0xFF, data[2]);
            Assert.Equal(0xFF, data[3]);
            Assert.Equal(0x07, data[4]);
        }

        [Fact]
        public void multi_byte_size_has_zero_in_next_position()
        {
            var data = WriteLengthData(2097150);
            Assert.Equal(data[3], 0);
        }

        [Fact]
        public void returns_index_into_buffer()
        {
            var target = new SnappyCompressor();
            var data = new byte[10];

            var result = target.WriteUncomressedLength(data, 0, 2097150);

            Assert.Equal(3, result);
        }

        private static byte[] WriteLengthData(int value)
        {
            var target = new SnappyCompressor();
            var data = new byte[10];

            target.WriteUncomressedLength(data, 0, value);
            return data;
        }
    }
}
