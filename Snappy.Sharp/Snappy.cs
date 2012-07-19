using System;
using System.IO;

namespace Snappy.Sharp
{
    public static class Snappy
    {
        internal const int LITERAL = 0;
        internal const int COPY_1_BYTE_OFFSET = 1; // 3 bit length + 3 bits of offset in opcode
        internal const int COPY_2_BYTE_OFFSET = 2;
        internal const int COPY_4_BYTE_OFFSET = 3;

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
