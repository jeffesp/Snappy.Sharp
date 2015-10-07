using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            var sizes = target2.ReadUncompressedLength(result, 0);
            var bytes = new byte[sizes[0]];
            target2.Decompress(result, 0 + sizes[1], size - sizes[1], bytes, 0, sizes[1]);

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
