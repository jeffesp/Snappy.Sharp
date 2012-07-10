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

            int resultSize = target.MaxCompressedLength(data.Length);
            byte[] compressed = new byte[resultSize];

            int result = target.Compress(data, 0, data.Length, compressed);

            Assert.Equal(data.Length, result); 
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
