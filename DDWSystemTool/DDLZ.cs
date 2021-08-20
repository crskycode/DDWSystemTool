using System;

namespace DDWSystemTool
{
    static class DDLZ
    {
        public static byte[] Decompress(byte[] input, int size)
        {
            byte[] output = new byte[size];

            int remaining = size;

            int in_p = 0;
            int out_p = 0;

            while (remaining > 0)
            {
                int flag = input[in_p++];
                int count;

                if (flag >= 0x20)
                {
                    int offset;
                    int length;

                    if ((flag & 0x80) == 0)
                    {
                        if ((flag & 0x60) == 0x20)
                        {
                            offset = (flag >> 2) & 7;
                            length = flag & 3;
                        }
                        else if ((flag & 0x60) == 0x40)
                        {
                            offset = input[in_p++];
                            length = (flag & 0x1F) + 4;
                        }
                        else
                        {
                            int flag1 = input[in_p++];
                            int flag2 = input[in_p++];

                            offset = flag1 | ((flag & 0x1F) << 8);

                            if (flag2 == 0xFE)
                            {
                                length = input[in_p++] << 8;
                                length |= input[in_p++];
                                length += 0x102;
                            }
                            else if (flag2 == 0xFF)
                            {
                                length = input[in_p++] << 8;
                                length = (length | input[in_p++]) << 8;
                                length = (length | input[in_p++]) << 8;
                                length |= input[in_p++];
                            }
                            else
                            {
                                length = flag2 + 4;
                            }
                        }
                    }
                    else
                    {
                        length = (flag >> 5) & 3;
                        offset = input[in_p++] | ((flag & 0x1F) << 8);
                    }

                    count = length + 3;

                    int ofs = out_p - offset - 1;
                    for (int i = 0; i < count; i++)
                        output[out_p++] = output[ofs++];
                }
                else if (flag >= 0x1D)
                {
                    if (flag == 0x1D)
                    {
                        count = input[in_p++] + 0x1E;
                    }
                    else if (flag == 0x1E)
                    {
                        count = input[in_p++] << 8;
                        count |= input[in_p++];
                        count += 0x11E;
                    }
                    else
                    {
                        count = input[in_p++] << 8;
                        count = (count | input[in_p++]) << 8;
                        count = (count | input[in_p++]) << 8;
                        count |= input[in_p++];
                    }

                    Array.Copy(input, in_p, output, out_p, count);

                    out_p += count;
                    in_p += count;
                }
                else
                {
                    count = flag + 1;

                    Array.Copy(input, in_p, output, out_p, count);

                    out_p += count;
                    in_p += count;
                }

                remaining -= count;
            }

            return output;
        }

        public static byte[] Compress(byte[] input)
        {
            throw new NotImplementedException();
        }
    }
}
