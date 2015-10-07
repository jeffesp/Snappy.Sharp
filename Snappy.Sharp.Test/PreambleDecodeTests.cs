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

            var result = ReadLengthData(data);
            Assert.Equal(64, result[0]);
        }

        [Fact]
        public void decodes_multi_bytes()
        {
            byte[] data = new byte[10];
            data[0] = 0xFE;
            data[1] = 0xFF;
            data[2] = 0x7F; 

            var result = ReadLengthData(data);
            Assert.Equal(2097150, result[0]);
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

            var result = ReadLengthData(data);
            Assert.Equal(int.MaxValue, result[0]);
        }

        private static int[] ReadLengthData(byte[] data)
        {
            var target = new SnappyDecompressor();
            return target.ReadUncompressedLength(data, 0);
        }
    }
}
