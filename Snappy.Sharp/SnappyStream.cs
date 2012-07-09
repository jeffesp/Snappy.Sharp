using System;
using System.IO;
using System.IO.Compression;

namespace Snappy.Sharp
{
    // Modeled after System.IO.Compression.DeflateStream in the framework
    public class SnappyStream : Stream
    {
        private readonly Stream stream;
        private readonly CompressionMode compressionMode;
        private readonly bool leaveStreamOpen;

        private SnappyCompressor compressor;
        private SnappyDecompressor decompressor;

        public SnappyStream(Stream s, CompressionMode mode) : this(s, mode, false)
        {
        }

        public SnappyStream(Stream s, CompressionMode mode, bool leaveOpen)
        {
            stream = s;
            compressionMode = mode;
            leaveStreamOpen = leaveOpen;

            if (compressionMode == CompressionMode.Decompress)
            {
                if (!stream.CanRead)
                    throw new InvalidOperationException("Trying to decompress and cannot read stream.");

                decompressor =  new SnappyDecompressor();
            }
            if (compressionMode == CompressionMode.Compress)
            {
                if (!stream.CanWrite)
                    throw new InvalidOperationException("Trying to compress and cannot write stream.");

                compressor = new SnappyCompressor();
            }

        }

        /// <summary>
        /// Provides access to the underlying (compressed) <see cref="T:System.IO.Stream"/>.
        /// </summary>
        public Stream BaseStream { get { return stream; } }

        public override bool CanRead
        {
            get { return stream != null && compressionMode == CompressionMode.Decompress && stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return stream != null && compressionMode == CompressionMode.Compress && stream.CanWrite; }
        }

        public override void Flush()
        {
            if (stream != null) 
                stream.Flush();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }
        public override int EndRead(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }
        public override void EndWrite(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (leaveStreamOpen)
            {
                ;
            }
            else
            {
                ;
            }
        }

        /// <summary>
        /// This operation is not supported and always throws a <see cref="T:System.NotSupportedException" />.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">This operation is not supported on this stream.</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This operation is not supported and always throws a <see cref="T:System.NotSupportedException" />.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">This operation is not supported on this stream.</exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This property is not supported and always throws a <see cref="T:System.NotSupportedException" />.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">This property is not supported on this stream.</exception>
        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// This property is not supported and always throws a <see cref="T:System.NotSupportedException" />.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">This property is not supported on this stream.</exception>
        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

    }
}
