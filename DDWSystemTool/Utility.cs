using System;
using System.IO;

namespace DDWSystemTool
{
    static class Utility
    {
        public static string AutoFileNameExtension(byte[] data, string name)
        {
            // BMP
            if (data.Length > 6)
            {
                if (data[0] == 'B' && data[1] == 'M')
                {
                    if (BitConverter.ToInt32(data, 2) == data.Length)
                    {
                        return name + ".bmp";
                    }
                }
            }

            // TGA
            if (data.Length > 18)
            {
                // Color Map Type
                if (data[1] == 0 || data[1] == 1)
                {
                    // Image Type
                    if (data[2] == 0 || // No image data included.
                        data[2] == 1 || // Uncompressed, color-mapped images.
                        data[2] == 2 || // Uncompressed, RGB images.
                        data[2] == 3 || // Uncompressed, black and white images.
                        data[2] == 9 || // Runlength encoded color-mapped images.
                        data[2] == 10 || // Runlength encoded RGB images.
                        data[2] == 11 || // Compressed, black and white images.
                        data[2] == 32 || // Compressed color-mapped data, using Huffman, Delta, and runlength encoding.
                        data[2] == 33) // Compressed color-mapped data, using Huffman, Delta, and runlength encoding.  4-pass quadtree-type process.
                    {
                        // Pixel Depth
                        if (data[16] == 8 || data[16] == 16 || data[16] == 24 || data[16] == 32)
                        {
                            return name + ".tga";
                        }
                    }
                }
            }

            // OGG
            if (data.Length > 16)
            {
                if (data[0] == 'O' && data[1] == 'g' && data[2] == 'g' && data[3] == 'S')
                {
                    return name + ".ogg";
                }
            }

            // WAV
            if (data.Length > 16)
            {
                if (data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F')
                {
                    if (data[8] == 'W' && data[9] == 'A' && data[10] == 'V' && data[11] == 'E')
                    {
                        return name + ".wav";
                    }
                }
            }

            // HXB
            if (data.Length > 16)
            {
                if (data[0] == 'D' && data[1] == 'D' && data[2] == 'W')
                {
                    if (data[4] == 'H' && data[5] == 'X' && data[6] == 'B')
                    {
                        return name + ".hxb";
                    }
                }
            }

            return name;
        }

        public static bool PathIsFolder(string path)
        {
            return new FileInfo(path).Attributes.HasFlag(FileAttributes.Directory);
        }
    }
}
