using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xunit;
using Xunit.Extensions;

namespace Snappy.Sharp.Test
{
    public class SnappyCompressionTests
    {
        [Fact]
        public void compress_returns_bytes_copied()
        {
            var data = Encoding.Default.GetBytes("ThisThisThisThisThisThisThisThisThisThisThisThisThisThisThisThisThisThisThisThisThisThisThisThisThis"); 
            var target = new SnappyCompressor();

            int compressedSize = target.MaxCompressedLength(data.Length);
            var compressed = new byte[compressedSize];

            int result = target.Compress(data, 0, data.Length, compressed);

            Assert.True(result < compressedSize);

            var x = new SnappyDecompressor();

            var bytes = x.Decompress(compressed, 0, result);
            Console.Write(Encoding.Default.GetString(bytes));
        }

        [Fact]
        public void compress_random_data()
        {
            var data = GetRandomData(4096);
            var target = new SnappyCompressor();

            int compressedSize = target.MaxCompressedLength(data.Length);
            var compressed = new byte[compressedSize];

            int result = target.Compress(data, 0, data.Length, compressed);

            Assert.True(result < compressedSize); 
            var x = new SnappyDecompressor();

            var bytes = x.Decompress(compressed, 0, result);
            Assert.Equal(data, bytes);
        }

        [Fact]
        public void compress_multiple_blocks()
        {
            var data = GetRandomData(1 << 20); // 1MB in 32k blocks
            var target = new SnappyCompressor();

            int compressedSize = target.MaxCompressedLength(data.Length);
            var compressed = new byte[compressedSize];

            int result = target.Compress(data, 0, data.Length, compressed);

            Assert.True(result < compressedSize); 
            var x = new SnappyDecompressor();

            var bytes = x.Decompress(compressed, 0, result);
            Assert.Equal(data, bytes);
        }


        [Theory]
        [PropertyData("CompressedDataSizes")]
        public void compress_writes_uncompressed_length_first(int dataSize, int storageBytes)
        {
            var data = GetRandomData(dataSize);
            var target = new SnappyCompressor();

            int compressedSize = target.MaxCompressedLength(data.Length);
            var compressed = new byte[compressedSize];

            target.Compress(data, 0, data.Length, compressed);

            Assert.Equal(dataSize, compressed[0]);
        }

        [Theory]
        [PropertyData("TagValues")]
        public void emit_literal_tag_byte_counts(int dataSize, byte tagByteValue, int resultSizeExtenstion)
        {
            int outputPosition = (int)EmitLiteralTag(dataSize, resultSizeExtenstion)[0];
            Assert.Equal(resultSizeExtenstion, outputPosition);
        }

        [Theory]
        [PropertyData("TagValues")]
        public void emit_literal_tag_byte_values(int dataSize, byte tagByteValue, int resultSizeExtenstion)
        {
            byte[] result = (byte[])EmitLiteralTag(dataSize, resultSizeExtenstion)[1];
            Assert.Equal(tagByteValue, result[0]);
        }

        [Theory]
        [PropertyData("DataValues")]
        public void emit_literal_copies_bytes_to_destination(int dataSize, byte tagByteValue, int resultSizeExtension)
        {
            var target = new SnappyCompressor();
            var data = GetRandomData(dataSize);
            var result = new byte[target.MaxCompressedLength(dataSize)];

            var size = target.EmitLiteral(result, 0, data, 0, dataSize, true);

            Assert.Equal(data, result.Skip(size - dataSize).Take(dataSize));
        }

        private static object[] EmitLiteralTag(int dataSize, int resultSizeExtenstion)
        {
            var target = new SnappyCompressor();
            var result = new byte[1 + resultSizeExtenstion];

            int outputPosition = target.EmitLiteralTag(result, 0, dataSize);
            return new object[] { outputPosition, result};
        }

        private byte[] GetRandomData(int count = 100)
        {
            var r = new Random();
            var result = new byte[count];
            
            r.NextBytes(result);

            return result;
        }

        public static IEnumerable<object[]> TagValues
        {
            get
            {
                return new List<object[]>
                       {
                           new object[] {8, (byte)0x20, 1},
                           new object[] {24, (byte)0x60, 1},
                           new object[] {96, (byte)0xF0, 2},
                           new object[] {256, (byte)0xF4, 3},
                           new object[] {65536, (byte)0xF8, 4},
                           new object[] {int.MaxValue, (byte)0xFC, 5},
                       };
            }
        }

        public static IEnumerable<object[]> DataValues
        {
            get
            {
                return new List<object[]>
                       {
                           new object[] {16, (byte)0x20, 1},
                           new object[] {24, (byte)0x60, 1},
                           new object[] {96, (byte)0xF0, 2},
                           new object[] {256, (byte)0xF4, 3},
                           new object[] {65536, (byte)0xF8, 4},
                       };
            }
        }

        public static IEnumerable<object[]> CompressedDataSizes 
        {
            get
            {
                return new List<object[]>
                       {
                           new object[] {16, 1},
                           new object[] {24, 1},
                           new object[] {96, 1},
                           new object[] {256, 2},
                           new object[] {65536, 3},
                       };
            }
        }
    }
}
