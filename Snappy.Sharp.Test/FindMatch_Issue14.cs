using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Snappy.Sharp.Test
{
    public class FindMatch_Issue14
    {
        [Fact]
        public void TestFindMatchBug_32Bit()
        {
            var compressor = new SnappyCompressor(4);
            TestFindMatchBug_Internal(compressor);
        }

        [Fact]
        public void TestFindMatchBug_64Bit()
        {
            var compressor = new SnappyCompressor(8);
            TestFindMatchBug_Internal(compressor);
        }

        private void TestFindMatchBug_Internal(SnappyCompressor compressor)
        {
            var decompressor = new SnappyDecompressor();

            var size = 1024;

            var data = new byte[size];
            for (var i = 0; i < data.Length; ++i)
                data[i] = (byte)(i & 0xff);

            data[1021] = 5;
            data[1022] = 5;
            data[1023] = 5;

            var compressed = new byte[compressor.MaxCompressedLength(data.Length)];

            var compressedLength = compressor.Compress(data, 0, data.Length, compressed, 0);
            var decompressed = decompressor.Decompress(compressed, 0, compressedLength);
            for (var i = 0; i < data.Length; ++i)
                Assert.Equal(data[i],decompressed[i]);
            
        }
    }
}
