using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using Xunit;

namespace Snappy.Sharp.Test
{
    public class SnappyStreamTests
    {
        [Fact]
        public void stream_can_never_seek()
        {
            using (var ms = new MemoryStream())
            {
                Assert.True(ms.CanSeek);
                var target = new SnappyStream(ms, CompressionMode.Compress);
                Assert.False(target.CanSeek);
            }
        }

        [Fact] 
        public void stream_can_read_when_decompressing()
        {
            using (var ms = new MemoryStream())
            {
                var target = new SnappyStream(ms, CompressionMode.Decompress);
                Assert.True(target.CanRead);
            }
        }
        [Fact] 
        public void stream_cannot_read_when_compressing()
        {
            using (var ms = new MemoryStream())
            {
                var target = new SnappyStream(ms, CompressionMode.Compress);
                Assert.False(target.CanRead);
            }
        }
    }
}
