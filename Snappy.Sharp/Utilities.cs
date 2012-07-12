using System.Diagnostics;

namespace Snappy.Sharp
{
    internal class Utilities
    {
        /// <summary>
        /// Copies 64 bits (8 bytes) from source array starting at sourceIndex into dest array starting at destIndex.
        /// </summary>
        /// <param name="source">The source array.</param>
        /// <param name="sourceIndex">Index to start copying.</param>
        /// <param name="dest">The destination array.</param>
        /// <param name="destIndex">Index to start writing.</param>
        /// <remarks>The name comes from the original Snappy C++ source. I don't think there is a good way to look at 
        /// things in an aligned manner in the .NET Framework.</remarks>
        public unsafe static void UnalignedCopy64(byte[] source, int sourceIndex, byte[] dest, int destIndex)
        {
            fixed (byte* pSrc = &source[sourceIndex], pDest = &dest[destIndex])
            {
                *((long*)pDest) = *((long*)pSrc);
            }
        }
    }
}
