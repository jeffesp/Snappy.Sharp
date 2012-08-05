using System;
using System.IO;
using System.IO.Compression;

namespace Snappy.Sharp
{
    // Modeled after System.IO.Compression.DeflateStream in the framework
    public class SnappyStream : Stream
    {
        private Stream stream;
        private readonly CompressionMode compressionMode;
        private readonly bool leaveStreamOpen;
        private readonly bool writeChecksums;
        private static byte[] streamHeader = new byte[] { (byte)'s', (byte)'N', (byte)'a', (byte)'P', (byte)'p', (byte)'Y'};
        private const byte streamIdentifier = 0xff;
        private const byte compressedType = 0x00;
        private const byte uncompressedType = 0x01;


        private SnappyCompressor compressor;
        private SnappyDecompressor decompressor;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnappyStream"/> class.
        /// </summary>
        /// <param name="s">The stream.</param>
        /// <param name="mode">The compression mode.</param>
        public SnappyStream(Stream s, CompressionMode mode) : this(s, mode, false, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnappyStream"/> class.
        /// </summary>
        /// <param name="s">The stream.</param>
        /// <param name="mode">The compression mode.</param>
        /// <param name="leaveOpen">If set to <c>true</c> leaves the stream open when complete.</param>
        public SnappyStream(Stream s, CompressionMode mode, bool leaveOpen, bool checksum)
        {
            stream = s;
            compressionMode = mode;
            leaveStreamOpen = leaveOpen;
            writeChecksums = checksum;

            if (compressionMode == CompressionMode.Decompress)
            {
                if (!stream.CanRead)
                    throw new InvalidOperationException("Trying to decompress and underlying stream not readable.");

                decompressor =  new SnappyDecompressor();

                // TODO: check for header
            }
            if (compressionMode == CompressionMode.Compress)
            {
                if (!stream.CanWrite)
                    throw new InvalidOperationException("Trying to compress and underlying stream is not writable.");

                compressor = new SnappyCompressor();

                stream.WriteByte(streamIdentifier);
                stream.Write(streamHeader, 0, streamHeader.Length);
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
            if (compressionMode != CompressionMode.Decompress || decompressor == null)
                throw new InvalidOperationException("Cannot read if not set to decompression mode.");

            // TODO: could probably speed this up with a reusable buffer here.
            byte[] decompressed = decompressor.Decompress(buffer, offset, count);
            stream.Write(decompressed, 0, decompressed.Length);

            return stream.Read(buffer, offset, count);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }
        public override void EndWrite(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        byte[] compressed = new byte[1<<16];
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (compressionMode != CompressionMode.Compress || compressor == null)
                throw new InvalidOperationException("Cannot write when set to compression mode.");
            if (count > 32768)
                throw new InvalidOperationException("Cannot write more than 32768 bytes.");

            stream.WriteByte(compressedType);
            compressor.WriteUncomressedLength(compressed, 1, count);
            int compressedLength = compressor.CompressInternal(buffer, offset, count, compressed, 2);

            stream.Write(compressed, 0, compressedLength + 3);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && stream != null)
                {
                    Flush();
                    if (compressionMode == CompressionMode.Compress && stream != null)
                    {
                        // Make sure all data written
                    }
                }
            }
            finally
            {
                try
                {
                    if (disposing && !leaveStreamOpen && stream != null)
                    {
                        stream.Close();
                    }
                }
                finally
                {
                    stream = null;
                    base.Dispose(disposing);
                }
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
