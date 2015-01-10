using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Xunit;
using Xunit.Extensions;

namespace Snappy.Sharp.Test
{
    public class RoundtripTests
    {
        [Theory]
        [PropertyData("DataSources")]
        public void round_trip_returns_original_data(string fileName)
        {
            byte[] uncompressed = File.ReadAllBytes(fileName);
            var target = new SnappyCompressor();
            var result = new byte[target.MaxCompressedLength(uncompressed.Length)];
            int size = target.Compress(uncompressed, 0, uncompressed.Length, result);


            var target2 = new SnappyDecompressor();
            int offset = 0;
            int outsize = target2.ReadUncompressedLength(result, ref offset);
            var bytes = new byte[size];
            target2.Decompress(result, 0 + offset, size - offset, bytes, 0, outsize);

            Assert.Equal(uncompressed, bytes);
        }

        public static IEnumerable<object[]> DataSources
        {
            get
            {
                var files = Directory.GetFiles(@"..\..\..\testdata");
                return files.Select(f => new object[] {f});
            } 
        }
    }
}
