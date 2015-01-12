using System.Text;
using Xunit;

namespace Snappy.Sharp.Test
{
    public class DecompressionReadCopies
    {
        [Fact]
        public void read_copy1_byte_does_copy()
        {
        }

        [Fact]
        public void read_copy2_byte_does_copy()
        {

        }

        [Fact]
        public void read_copy4_byte_does_copy()
        {

        }

        [Fact]
        public void will_read_multiple_copy_and_repeats_the_literal()
        {
            //from the description:
            //
            //As in most LZ77-based compressors, the length can be larger than the offset,
            //yielding a form of run-length encoding (RLE). For instance,
            //"xababab" could be encoded as
            //<literal: "xab"> <copy: offset=2 length=4>

            //Next to last byte is set to have an offset of 2 and length of 4. This should 
            //make it duplicate the two bytes that start the input.

            byte[] input = new byte[] {0x41, 0x0};

            SnappyDecompressor target = new SnappyDecompressor();

            byte[] output = new byte[7];
            output[0] = 0x78;
            output[1] = 0x61;
            output[2] = 0x62;
            target.Decompress(input, 0, 2, output, 3, 7);

            Assert.Equal("xababab", Encoding.ASCII.GetString(output));
        }
    }
}