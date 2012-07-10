using System;
using System.IO;
using System.IO.Compression;

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

        [Fact]
        public void stream_must_be_writable_when_compressing()
        {
            byte[] test = new byte[1024];
            using (var ms = new MemoryStream(test, false))
            {
                Assert.Throws<InvalidOperationException>(() => new SnappyStream(ms, CompressionMode.Compress));
            }
        }

        [Fact]
        public void stream_must_be_readable_when_decompressing()
        {
            var ms = new MemoryStream();
            ms.Dispose(); // A disposed stream is not null, but is no longer readable 
            Assert.Throws<InvalidOperationException>(() => new SnappyStream(ms, CompressionMode.Decompress));
        }

        [Fact]
        public void underlying_stream_closed_on_dispose()
        {
            var ms = new MemoryStream();
            using (var target = new SnappyStream(ms, CompressionMode.Compress))
            {
            }
            Assert.Throws<ObjectDisposedException>(() => ms.Capacity);
        }

        [Fact]
        public void underlying_stream_not_closed_when_contructor_says_no()
        {
            var ms = new MemoryStream();
            using (var target = new SnappyStream(ms, CompressionMode.Compress, true))
            {
            }
            Assert.True(ms.CanWrite && ms.CanRead);
        }
    }
}
