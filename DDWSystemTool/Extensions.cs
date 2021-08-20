using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DDWSystemTool
{
    static class Extensions
    {
        public static short ReadInt16BE(this BinaryReader @this)
        {
            return Endian.Reverse(@this.ReadInt16());
        }

        public static ushort ReadUInt16BE(this BinaryReader @this)
        {
            return Endian.Reverse(@this.ReadUInt16());
        }

        public static int ReadInt32BE(this BinaryReader @this)
        {
            return Endian.Reverse(@this.ReadInt32());
        }

        public static uint ReadUInt32BE(this BinaryReader @this)
        {
            return Endian.Reverse(@this.ReadUInt32());
        }

        public static string ReadAnsiString(this BinaryReader @this, Encoding encoding = null)
        {
            var buf = new List<byte>(256);

            for (var b = @this.ReadByte(); b != 0; b = @this.ReadByte())
            {
                buf.Add(b);
            }

            if (buf.Count == 0)
            {
                return string.Empty;
            }

            if (encoding == null)
            {
                encoding = Encoding.GetEncoding("shift_jis");
            }

            return encoding.GetString(buf.ToArray());
        }

        public static string ReadUnicodeString(this BinaryReader @this)
        {
            var sb = new StringBuilder();

            while (true)
            {
                var c = (char)@this.ReadUInt16();

                if (c == 0)
                {
                    break;
                }

                sb.Append(c);
            }

            if (sb.Length == 0)
            {
                return string.Empty;
            }

            return sb.ToString();
        }

        public static void WriteUnicodeString(this BinaryWriter @this, string value)
        {
            @this.Write(Encoding.Unicode.GetBytes(value));
            @this.Write((short)0);
        }

        public static bool IsNull(this object @this)
        {
            return @this == null;
        }

        static readonly HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong)
        };

        public static bool IsNumber(this object @this)
        {
            return (@this != null) && NumericTypes.Contains(@this.GetType());
        }

        public static bool IsString(this object @this)
        {
            return (@this != null) && @this.GetType() == typeof(string);
        }

        public static bool HasFlag(this byte test, int bits)
        {
            return (test & bits) != 0;
        }

        public static bool HasFlag(this int test, int bits)
        {
            return (test & bits) != 0;
        }
    }
}
