using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xunit;

namespace Snappy.Sharp.Test
{
    public class UtilitiesTests
    {
        [Fact]
        public void copies_data_from_source_to_dest()
        {
            byte[] source = new byte[12];
            byte[] dest = new byte[8];

            new Random().NextBytes(source);

            Utilities.UnalignedCopy64(source, 0, dest, 0);

            Assert.Equal(source.Take(8).ToArray(), dest);
        }

        [Fact]
        public void data_copies_do_not_need_to_be_aligned()
        {
            byte[] source = new byte[12];
            byte[] dest = new byte[8];

            new Random().NextBytes(source);

            Utilities.UnalignedCopy64(source, 3, dest, 0);

            Assert.Equal(source.Skip(3).Take(8).ToArray(), dest);
        }

    }
}
