using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Snappy.Sharp.Test
{
    public class PreambleDecodeTests
    {
        [Fact]
        public void decodes_one_byte()
        {
            byte[] data = new byte[1];
            data[0] = 0x7F;

            var result = ReadLengthData(data);
            Assert.Equal(128, result);
        }

        [Fact]
        public void decodes_two_bytes()
        {
            byte[] data = new byte[2];
            data[0] = 0xF7;
            data[1] = 0x7F; 

            var result = ReadLengthData(data);
            Assert.Equal(128, result);
        }

        [Fact]
        public void decodes_three_bytes()
        {
            byte[] data = new byte[3];
            data[0] = 0xF7;
            data[1] = 0xF7;
            data[2] = 0x7F; 

            var result = ReadLengthData(data);
            Assert.Equal(128, result);
        }

        [Fact]
        public void decodes_four_bytes()
        {
            byte[] data = new byte[4];
            data[0] = 0xF7;
            data[1] = 0xF7;
            data[2] = 0x7F; 
            data[3] = 0x7F; 

            var result = ReadLengthData(data);
            Assert.Equal(128, result);
        }

        [Fact]
        public void decodes_five_bytes()
        {
            byte[] data = new byte[5];
            data[0] = 0xF7;
            data[1] = 0xF7;
            data[2] = 0x7F; 
            data[3] = 0x7F; 
            data[4] = 0x7F; 

            var result = ReadLengthData(data);
            Assert.Equal(128, result);
        }

        private static int ReadLengthData(byte[] data)
        {
            var target = new SnappyDecompressor();

            return target.ReadUncompressedLength(data, 0);
        }
    }
}
