using System.Text;

using Xunit;

namespace Snappy.Sharp.Test
{
    public class DecompressionReadLiterals
    {
        [Fact]
        public void read_literal_length_in_tag_copies_literal()
        {
            byte[] input = new byte[] {0x08, 0x78, 0x61, 0x62};

            SnappyDecompressor target = new SnappyDecompressor();

            byte[] output = new byte[3];
            target.Decompress(input, 0, 4, output, 0, 3);

            Assert.Equal(Encoding.ASCII.GetString(output), "xab");
        }

        [Fact]
        public void read_literal_length_in_one_byte_copies_literal()
        {
            byte[] input = new byte[] {0xF0, 0x02, 0x78, 0x61, 0x62};

            SnappyDecompressor target = new SnappyDecompressor();

            byte[] output = new byte[3];
            target.Decompress(input, 0, 5, output, 0, 3);

            Assert.Equal(Encoding.ASCII.GetString(output), "xab");
        }

        [Fact]
        public void read_literal_length_in_two_bytes_copies_literal()
        {
            byte[] input = new byte[] {0xF4, 0x02, 0x00, 0x78, 0x61, 0x62};

            SnappyDecompressor target = new SnappyDecompressor();

            byte[] output = new byte[3];
            target.Decompress(input, 0, 6, output, 0, 3);

            Assert.Equal(Encoding.ASCII.GetString(output), "xab");
        }

        [Fact]
        public void read_literal_length_in_three_bytes_copies_literal()
        {
            byte[] input = new byte[] {0xF8, 0x02, 0x00, 0x00, 0x78, 0x61, 0x62};

            SnappyDecompressor target = new SnappyDecompressor();

            byte[] output = new byte[3];
            target.Decompress(input, 0, 7, output, 0, 3);

            Assert.Equal(Encoding.ASCII.GetString(output), "xab");
        }

        [Fact]
        public void read_literal_length_in_four_bytes_copies_literal()
        {
            byte[] input = new byte[] {0xFC, 0x02, 0x00, 0x00, 0x00, 0x78, 0x61, 0x62};

            SnappyDecompressor target = new SnappyDecompressor();

            byte[] output = new byte[3];
            target.Decompress(input, 0, 8, output, 0, 3);

            Assert.Equal(Encoding.ASCII.GetString(output), "xab");
        }
    }
}
