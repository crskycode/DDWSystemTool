using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDWSystemTool
{
    static class Endian
    {
        public static long Reverse(long value)
        {
            return (((long)Reverse((int)value) & 0xFFFFFFFF) << 32)
                    | ((long)Reverse((int)(value >> 32)) & 0xFFFFFFFF);
        }

        public static ulong Reverse(ulong value)
        {
            return (((ulong)Reverse((uint)value) & 0xFFFFFFFF) << 32)
                    | ((ulong)Reverse((uint)(value >> 32)) & 0xFFFFFFFF);
        }

        public static int Reverse(int value)
        {
            return (((int)Reverse((short)value) & 0xFFFF) << 16)
                    | ((int)Reverse((short)(value >> 16)) & 0xFFFF);
        }

        public static uint Reverse(uint value)
        {
            return (((uint)Reverse((ushort)value) & 0xFFFF) << 16)
                    | ((uint)Reverse((ushort)(value >> 16)) & 0xFFFF);
        }

        public static short Reverse(short value)
        {
            return (short)((((int)value & 0xFF) << 8) | (int)((value >> 8) & 0xFF));
        }

        public static ushort Reverse(ushort value)
        {
            return (ushort)((((int)value & 0xFF) << 8) | (int)((value >> 8) & 0xFF));
        }
    }
}
