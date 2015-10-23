using System;

using Xunit;

namespace Snappy.Sharp.Test
{
    public class PreambleDecodeTests
    {
        [Fact]
        public void decodes_one_byte()
        {
            byte[] data = new byte[10];
            data[0] = 0x40;

            int offset = 0;
            var target = new SnappyDecompressor();
            int result = data.FromVarInt(ref offset);
            Assert.Equal(64, result);
            Assert.Equal(1, offset);
        }

        [Fact]
        public void decodes_multi_bytes()
        {
            byte[] data = new byte[10];
            data[0] = 0xFE;
            data[1] = 0xFF;
            data[2] = 0x7F; 

            int offset = 0;
            var target = new SnappyDecompressor();
            int result = data.FromVarInt(ref offset);
            Assert.Equal(2097150, result);
            Assert.Equal(3, offset);
        }

        [Fact]
        public void int_maxvalue_decoded()
        {
            byte[] data = new byte[10];
            data[0] = 0xFF;
            data[1] = 0xFF;
            data[2] = 0xFF; 
            data[3] = 0xFF; 
            data[4] = 0x7;

            int offset = 0;
            var target = new SnappyDecompressor();
            int result = data.FromVarInt(ref offset);
            Assert.Equal(Int32.MaxValue, result);
            Assert.Equal(5, offset);
        }

        [Fact]
        public void decompression_read_uncompressed_length_throws_when_no_data()
        {
            byte[] input = new byte[0];
            int offset = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() => input.FromVarInt(ref offset));
        }

        [Fact]
        public void decompression_read_uncompressed_length_throws_when_offset_exceeds_data()
        {
            byte[] input = new byte[10];
            int offset = 10;
            Assert.Throws<ArgumentOutOfRangeException>(() => input.FromVarInt(ref offset));
        }
    }
}
