using System;
using System.IO;

namespace Snappy.Sharp
{
    public static class Snappy
    {
        public static long MaxCompressedLength(long sourceLength)
        {
            return 0;
        }

        public static void Compress(byte[] uncompressed, byte[] compressed)
        {
            throw new NotImplementedException();
        }

        public static void Compress(Stream uncompressed, SnappyStream compressed)
        {
            throw new NotImplementedException();
        }

        public static long GetUncompressedLength(byte[] compressed, int offset)
        {
            return 0;
        }

        public static void Uncompress(byte[] compressed, byte[] uncompressed)
        {
            throw new NotImplementedException();
        }

        public static void Uncompress(SnappyStream compressed, StreamWriter uncompressed)
        {
            throw new NotImplementedException();
        }
    }
}
