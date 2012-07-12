using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xunit;

namespace Snappy.Sharp.Test
{
    public class SnappyCompressionTests
    {
        [Fact]
        public void compress_returns_bytes_copied()
        {
            var data = GetRandomData();
            var target = new SnappyCompressor();

            int compressedSize = target.MaxCompressedLength(data.Length);
            var compressed = new byte[compressedSize];

            int result = target.Compress(data, 0, data.Length, compressed);

            Assert.Equal(data.Length, result); 
        }

        [Fact]
        public void compress_writes_uncompressed_length_first()
        {
            var data = GetRandomData(64);
            var target = new SnappyCompressor();

            int compressedSize = target.MaxCompressedLength(data.Length);
            var compressed = new byte[compressedSize];

            target.Compress(data, 0, data.Length, compressed);

            Assert.Equal(64, compressed[0]);
        }

        private byte[] GetRandomData(int count = 100)
        {
            var r = new Random();
            var result = new byte[count];
            
            r.NextBytes(result);

            return result;
        }
    }
}
