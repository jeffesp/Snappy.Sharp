using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xunit;

namespace Snappy.Sharp.Test
{
    public class SnappyDecompressionTests
    {
        [Fact]
        public void decompression_read_uncompressed_length_gives_back_count_of_bytes_making_up_length()
        {
        }

        [Fact]
        public void decompression_read_literal_outputs_literal()
        {

        }

        [Fact]
        public void decompression_read_copy1_byte_does_copy()
        {

        }

        [Fact]
        public void decompression_read_copy2_byte_does_copy()
        {

        }

        [Fact]
        public void decompression_read_copy4_byte_does_copy()
        {

        }

        [Fact]
        public void decompression_will_read_multiple_copy_and_repeats_the_literal()
        {
            //from the description:
            //
            //As in most LZ77-based compressors, the length can be larger than the offset,
            //yielding a form of run-length encoding (RLE). For instance,
            //"xababab" could be encoded as
            //<literal: "xab"> <copy: offset=2 length=4>

            //Next to last byte is set to have an offset of 2 and length of 4. This should 
            //make it duplicate the two bytes that start the input.

            byte[] input = new byte[] {0x0C, 0x78, 0x61, 0x62, 0x41, 0x0};

            SnappyDecompressor target = new SnappyDecompressor();

            byte[] output = new byte[7];
            target.Decompress(input, 0, 8, output, 0, 7);

            Assert.Equal(Encoding.ASCII.GetString(output), "xababab");
        }
    }
}
