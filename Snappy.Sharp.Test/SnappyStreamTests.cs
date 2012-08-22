using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

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
            using (var ms = new MemoryStream(new byte[] {255, 115, 78, 97, 80, 112, 89, 0, 0, 0, 8, 0, 100, 0, 254, 1, 0, 130, 1, 0, 0 }))
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
            using (var target = new SnappyStream(ms, CompressionMode.Compress, true, false))
            {
            }
            Assert.True(ms.CanWrite && ms.CanRead);
        }

        [Fact]
        public void identifier_and_header_written_on_compression_construction()
        {
            var ms = new MemoryStream();
            using (var target = new SnappyStream(ms, CompressionMode.Compress, true, false))
            {
            }
            Assert.Equal(new byte[] {0xff, (byte)'s', (byte)'N', (byte)'a', (byte)'P', (byte)'p', (byte)'Y'}, ms.GetBuffer().Take((int) ms.Length));
        }

        [Fact]
        public void data_written_to_stream()
        {
            var ms = new MemoryStream();
            using (var target = new SnappyStream(ms, CompressionMode.Compress, true, false))
            {
                byte[] buffer = new byte[100];
                target.Write(buffer, 0, buffer.Length); 
            }
            Assert.Equal(new byte[] { 255, 115, 78, 97, 80, 112, 89, 0, 0, 0, 8, 0, 100, 0, 254, 1, 0, 130, 1, 0, 0 }, ms.GetBuffer().Take((int) ms.Length));
        }

        [Fact]
        public void stream_with_valid_data_able_to_be_read()
        {

        }

        [Fact]
        public void stream_with_uncompressed_data_copied_out_to_buffer()
        {
            
        }

        [Fact]
        public void stream_with_compressed_data_is_decompressed()
        {
            
        }

        [Fact]
        public void stream_with_multiple_chunks_of_data_reads_multiple_when_requesting_more_data()
        {

        }

        [Fact]
        public void steam_reading_partial_chunk_will_return_more_from_same_chunk_on_next_read()
        {
            
        }

        [Fact]
        public void stream_read_returns_number_of_bytes_read__read_from_one_chunk()
        {

        }

        [Fact]
        public void stream_read_returns_number_of_bytes_read__read_from_multiple_chunks()
        {

        }

        [Fact]
        public void stream_read_throws_exception_on_invalid_chunk_type()
        {
        }

        [Fact]
        public void stream_read_throws_exception_on_chunk_length_too_long()
        {
            
        }

        [Fact]
        public void stream_write_throws_exception_on_chunk_length_too_long()
        {
            
            var ms = new MemoryStream();
            using (var target = new SnappyStream(ms, CompressionMode.Compress, true, false))
            {
                byte[] buffer = new byte[1 << 20];
                Assert.Throws<InvalidOperationException>(() => target.Write(buffer, 0, 1 << 20)); 
            }
        }

        [Fact]
        public void stream_writing_multiple_chunks()
        {

        }

        [Fact]
        public void stream_reading_multiple_chunks()
        {

        }
    }
}
