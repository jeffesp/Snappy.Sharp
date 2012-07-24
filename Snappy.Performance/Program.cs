#if false
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Snappy.Sharp;

namespace Snapp.Performance
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (string fileName in Directory.GetFiles(args[0]))
            {
                int size = 0;
                byte[] uncompressed = File.ReadAllBytes(fileName);
                var target = new SnappyCompressor();
                var s = Stopwatch.StartNew();
                for (int i = 0; i < 200; i++)
                {
                    s.Start();
                    var result = new byte[target.MaxCompressedLength(uncompressed.Length)];
                    size = target.Compress(uncompressed, 0, uncompressed.Length, result);
                    s.Stop();
                }

                Console.WriteLine(String.Format("{0,-20}\t{4:F1}x\t{1:F8}\t{2:F8}\t{3:F2}",
                                                Path.GetFileName(fileName),
                                                (((double)s.ElapsedMilliseconds / 1000) / 200), // convert milliseconds to seconds, divide by iterations 
                                                ((double)uncompressed.Length / 1024 / 1024), // convert B to MB 
                                                ((double)uncompressed.Length / 1024 / 1024) / (((double)s.ElapsedMilliseconds / 1000) / 200),
                                                (double)uncompressed.Length / size
                                      ));
            }
        }
    }
}
#else

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Snappy.Sharp;

namespace Snappy.Performance
{
    class Program
    {
        private class CompressionResult
        {
            public string FileName { get; set; }
            public long FileBytes { get; set; }
            public TimeSpan ElapsedTime { get; set; }
            public int Iterations { get; set; }
            public long CompressedSize { get; set; }

            public double CompresionPercentage
            {
                get { return ((double)CompressedSize/FileBytes)*100; }
            }

            public double Throughput
            {
                get { return ( (double)FileBytes * Iterations / (1 << 20)) / (ElapsedTime.TotalMilliseconds / 1e3 ); }
            }

            public override string ToString()
            {
                return String.Format("{0,-20}\t{1}\t{2:F1}%\t{3:F2}",
                                     Path.GetFileName(FileName),
                                     FileBytes,
                                     CompresionPercentage,
                                     Throughput);
            }
        }
        static CompressionResult RunCompression(string fileName, int iterations)
        {
            int size = 0;

            byte[] uncompressed = File.ReadAllBytes(fileName);

            var target = new SnappyCompressor();
            var s = new Stopwatch();

            for (int i = 0; i < iterations; i++)
            {
                s.Start();
                var result = new byte[target.MaxCompressedLength(uncompressed.Length)];
                size = target.Compress(uncompressed, 0, uncompressed.Length, result);
                s.Stop();
            }

            return new CompressionResult
                       {
                           FileName =  Path.GetFileName(fileName),
                           CompressedSize = size,
                           FileBytes = uncompressed.Length,
                           ElapsedTime = s.Elapsed,
                           Iterations = iterations
                       };
        }
        private static CompressionResult RunDecompression(string fileName, int iterations)
        {
            byte[] uncompressed = File.ReadAllBytes(fileName);
            var compressed = Sharp.Snappy.Compress(uncompressed);
            int size = compressed.Length;
            var target = new SnappyDecompressor();
            var s = new Stopwatch();

            for (int i = 0; i < iterations; i++)
            {
                s.Start();
                var result = target.Decompress(compressed, 0, compressed.Length);
                s.Stop();
            }

            return new CompressionResult
                       {
                           FileName =  Path.GetFileName(fileName),
                           CompressedSize = size,
                           FileBytes = uncompressed.Length,
                           ElapsedTime = s.Elapsed,
                           Iterations = iterations
                       };
        }
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("usage: Snappy.Performance.exe <directory of files to test>");
                return;
            }
            List<CompressionResult> results = new List<CompressionResult>();
            foreach (string fileName in Directory.GetFiles(args[0]))
            {
                results.Add(RunCompression(fileName, 1));
                results.Add(RunDecompression(fileName, 1));
            }

            foreach (var r in results) Console.WriteLine(r);
        }

    }
}
#endif
