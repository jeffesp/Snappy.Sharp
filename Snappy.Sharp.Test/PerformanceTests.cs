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
    public class PerformanceTests
    {        double nanosPerTick = (1000000000 / Stopwatch.Frequency);

        [Theory]
        [PropertyData("DataSources")]
        public void run(string fileName)
        {
            int size = 0;
            byte[] uncompressed = File.ReadAllBytes(fileName);

            var s = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
            {
                var target = new SnappyCompressor();
                var result = new byte[target.MaxCompressedLength(uncompressed.Length)];
                size = target.Compress(uncompressed, 0, uncompressed.Length, result);
            }
            s.Stop();

            Console.WriteLine(String.Format("{0,-20}\t{4:F1}x\t{1:F8}\t{2:F8}\t{3:F2}", 
                Path.GetFileName(fileName),
                (((double)s.ElapsedMilliseconds / 1000) / 200),
                ((double)uncompressed.Length / 1024 / 1024),
                ((double)uncompressed.Length / 1024 / 1024) / (((double)s.ElapsedMilliseconds / 1000) / 200),
                (double)uncompressed.Length/size
            ));
        }

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
            target2.Decompress(result, 0 + sizes[1], size - sizes[1], bytes, 0);

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
