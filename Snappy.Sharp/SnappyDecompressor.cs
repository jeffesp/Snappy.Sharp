using System;
using System.IO;

namespace Snappy.Sharp
{
    public class SnappyDecompressor
    {
        const int bitMask = 0x80;

        public byte[] Decompress(byte[] compressed, int compressedOffset, int compressedSize)
        {
            int sizeHeader = compressed.FromVarInt(ref compressedOffset);
            var data = new byte[sizeHeader];

            Decompress(compressed, compressedOffset, compressedSize + compressedOffset, data, 0, data.Length);

            return data;
        }
        public int Decompress(byte[] input, int inputOffset, int inputSize, byte[] output, int outputOffset, int outputLimit)
        {
            while (outputOffset < outputLimit)
            {
                Snappy.TagType currentTagType = ClassifyTag(input[inputOffset]);
                if (currentTagType == Snappy.TagType.Literal)
                {
                    var literalLength = input[inputOffset] >> 2;

                    int sourceOffset = 0;
                    // Literal with length directy encoded
                    if (literalLength < 60)
                    {
                        sourceOffset = 1;
                    }
                    else if (literalLength == 60)
                    {
                        literalLength = input[inputOffset + 1];
                        sourceOffset = 2;
                    }
                    else if (literalLength == 61)
                    {
                        literalLength = (input[inputOffset + 2] << 8) | input[inputOffset + 1];
                        sourceOffset = 3;
                    }
                    else if (literalLength == 62)
                    {
                        literalLength = (input[inputOffset + 3] << 16) | (input[inputOffset + 2] << 8) |
                                        input[inputOffset + 1];
                        sourceOffset = 4;
                    }
                    else if (literalLength == 63)
                    {
                        literalLength = (input[inputOffset + 4] << 24) | (input[inputOffset + 3] << 16) |
                                        (input[inputOffset + 2] << 8) | input[inputOffset + 1];
                        sourceOffset = 5;
                    }

                    literalLength = literalLength + 1;
                    if (sourceOffset == 0 || literalLength + inputOffset > outputLimit)
                        throw new InvalidDataException("Decoded literal-length that would exceed the source buffer size.");

                    Buffer.BlockCopy(input, inputOffset + sourceOffset, output, outputOffset, literalLength);
                    inputOffset = inputOffset + literalLength + sourceOffset;
                    outputOffset = outputOffset + literalLength;
                }
                else if (currentTagType == Snappy.TagType.Copy1ByteOffset)
                {
                    int length = 4 + ((input[inputOffset] >> 2) & 0x7);
                    int offset = input[inputOffset] >> 5 + input[inputOffset + 1];

                    outputOffset = CopyCopy(output, outputOffset, length, offset);
                    inputOffset += 2;
                }
                else if (currentTagType == Snappy.TagType.Copy2ByteOffset)
                {
                    int length = 1 + input[inputOffset] >> 2;
                    int offset = input[inputOffset+2] << 8 + input[inputOffset + 1];

                    outputOffset = CopyCopy(output, outputOffset, length, offset);
                    inputOffset += 3;
                }
                else if (currentTagType == Snappy.TagType.Copy4ByteOffset)
                {
                    int length = 1 + input[inputOffset] >> 2;
                    int offset = (input[inputOffset + 4] << 24) | (input[inputOffset + 3] << 16) |
                                 (input[inputOffset + 2] << 8) | input[inputOffset + 1];

                    outputOffset = CopyCopy(output, outputOffset, length, offset);
                    inputOffset += 5;
                }
            }
            return outputOffset;
        }

        static int CopyCopy(byte[] output, int outputOffset, int length, int offset)
        {
            int count = outputOffset + length;
            while (outputOffset < count)
            {
                output[outputOffset] = output[outputOffset - offset];
                outputOffset++;
            }
            return count;
        }

        Snappy.TagType ClassifyTag(byte input)
        {
            return (Snappy.TagType)(input & 0x03);
        }

    }
}
