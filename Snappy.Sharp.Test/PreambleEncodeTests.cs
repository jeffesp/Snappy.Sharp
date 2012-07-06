using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var data = WriteLengthData(10);
            Assert.Equal(data[0], 10);
        }

        [Fact]
        public void one_byte_size_has_zero_in_next_position()
        {
            var data = WriteLengthData(10);
            Assert.Equal(data[1], 0);
        }

        [Fact]
        public void two_byte_size()
        {
            var data = WriteLengthData(128);
            Assert.Equal(128, data[0]);
            Assert.Equal(1, data[1]);
        }

        [Fact]
        public void two_byte_size_has_zero_in_next_position()
        {
            var data = WriteLengthData(129);
            Assert.Equal(data[2], 0);
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
