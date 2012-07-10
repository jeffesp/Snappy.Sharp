using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using Xunit;

namespace Snappy.Sharp.Test
{
    public class SnappyStreamReadTests
    {
        [Fact]
        public void stream_decompress_read()
        {
            var data = new byte[] { 0, 1, 2, 3, 4 };
            using (var ms = new MemoryStream(data))
            {
                var result = new byte[5];
                var target = new SnappyStream(ms, CompressionMode.Decompress);
                target.Read(result, 0, 5);
                Assert.Equal(data, result);
            }
        }
    }
}
