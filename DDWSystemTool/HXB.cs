using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#pragma warning disable IDE0017 // Simplify object initialization

namespace DDWSystemTool.HXB
{
    class Binary
    {
        byte[] _data;
        StreamWriter _writer;

        public void Load(string filePath)
        {
            var data = File.ReadAllBytes(filePath);
            _data = Decrypt(data);
        }

        public void Save(string filePath)
        {
            var data = Decrypt(_data);
            File.WriteAllBytes(filePath, data);
        }

        public void Disasm(string filePath)
        {
            using (StreamWriter writer = File.CreateText(filePath))
            {
                var disasm = new Disassembler(_data);
                disasm.TextOutput = writer;
                disasm.Execute();
            }
        }

        public void ExportStrings(string filePath)
        {
            using (StreamWriter writer = File.CreateText(filePath))
            {
                _writer = writer;

                var disasm = new Disassembler(_data);
                disasm.OnCallScript = OnCallScript;
                disasm.Execute();

                _writer = null;
            }
        }

        public void ExportAllStrings(string filePath)
        {
            var disasm = new Disassembler(_data);
            disasm.Execute();

            var encoding = Encoding.GetEncoding("shift_jis");

            using (StreamWriter writer = File.CreateText(filePath))
            {
                foreach (var inst in disasm.Assembly.Instructs)
                {
                    if (inst.Inst != Instruction.ExprLoadImmStr)
                        continue;

                    string str;
                    if (disasm.IsUnicode)
                        str = Encoding.Unicode.GetString(_data, inst.Address + 1, inst.Length - 2 - 1);
                    else
                        str = encoding.GetString(_data, inst.Address + 1, inst.Length - 1 - 1);

                    if (str.Length == 0)
                        continue;

                    writer.WriteLine(string.Format("◇{0:X8}◇{1}", inst.Address, str));
                    writer.WriteLine(string.Format("◆{0:X8}◆{1}", inst.Address, str));
                    writer.WriteLine();
                }
            }
        }

        public void ImportStrings(string filePath)
        {
            Console.WriteLine("Disassembling binary");

            var disasm = new Disassembler(_data);
            disasm.Execute();

            Console.WriteLine("Loading translation file");

            var translated = new Dictionary<int, string>();

            using (StreamReader reader = File.OpenText(filePath))
            {
                int lineNo = 0;

                while (!reader.EndOfStream)
                {
                    int ln = lineNo;
                    var line = reader.ReadLine();
                    lineNo++;

                    if (line.Length == 0 || line[0] != '◆')
                        continue;

                    var m = Regex.Match(line, @"◆(\w+)◆(.+$)");

                    if (!m.Success || m.Groups.Count != 3)
                        throw new Exception($"Bad format at line: {ln}");

                    int offset = int.Parse(m.Groups[1].Value, NumberStyles.HexNumber);
                    var @string = m.Groups[2].Value;

                    translated.Add(offset, @string);
                }
            }

            // Rebuild binary

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write(_data, 0, 16);

            Console.WriteLine("Rebuilding binary");

            foreach (var inst in disasm.Assembly.Instructs)
            {
                inst.NewAddress = Convert.ToInt32(stream.Position);

                if (inst.Inst == Instruction.ExprLoadImmStr
                    && translated.TryGetValue(inst.Address, out string newstr))
                {
                    writer.Write((byte)0x80);
                    writer.WriteUnicodeString(newstr);
                }
                else
                {
                    writer.Write(_data, inst.Address, inst.Length);
                }
            }

            // Fix address

            Console.WriteLine("Fixing address");

            var instMapByAddr = disasm.Assembly.Instructs.ToDictionary(a => a.Address);

            foreach (var inst in disasm.Assembly.Instructs)
            {
                if (inst.Inst != Instruction.Addr)
                    continue;

                //Console.WriteLine($"Fix address at 0x{inst.Address:X8}");

                int oldTarget = BitToInt24(_data, inst.Address);
                int newTarget = instMapByAddr[oldTarget].NewAddress;
                var bytes = BitFromInt24(newTarget);

                stream.Position = inst.NewAddress;
                writer.Write(bytes);
            }

            Console.WriteLine("Updating header");

            // Update header
            var length = Convert.ToInt32(stream.Length);
            stream.Position = 8;
            writer.Write(BitFromInt24(length));

            // Finish
            _data = stream.ToArray();
        }

        static int BitToInt24(byte[] input, int index)
        {
            int val = input[index] << 8;
            val = (val | input[index + 1]) << 8;
            val |= input[index + 2];
            return val;
        }

        static byte[] BitFromInt24(int value)
        {
            var bytes = new byte[3];
            bytes[0] = (byte)((value & 0xFF0000) >> 16);
            bytes[1] = (byte)((value & 0xFF00) >> 8);
            bytes[2] = (byte)(value & 0xFF);
            return bytes;
        }

        void OnCallScript(EValue id, EValue[] args)
        {
            if (id.IsNumber)
            {
                var index = id.Number;

                if (index == 0x36 && args.Length > 1 && args[0].IsString)
                {
                    _writer.WriteLine(string.Format("◇{0:X8}◇{1}", args[0].Address, args[0].String));
                    _writer.WriteLine(string.Format("◆{0:X8}◆{1}", args[0].Address, args[0].String));
                    _writer.WriteLine();
                }

                if (index == 0x32 && args.Length > 2 && args[0].IsNumber && args[0].Number == 0x0C && args[1].IsString)
                {
                    _writer.WriteLine(string.Format("◇{0:X8}◇{1}", args[1].Address, args[1].String));
                    _writer.WriteLine(string.Format("◆{0:X8}◆{1}", args[1].Address, args[1].String));
                    _writer.WriteLine();
                }
            }
        }

        static byte[] Decrypt(byte[] data)
        {
            if (data.Length < 16)
            {
                throw new Exception("Not enough bytes");
            }

            var magic = BitConverter.ToUInt64(data, 0);

            if (magic != 0x42584875574444u)
            {
                throw new Exception("Wrong magic number");
            }

            int size = data[10] | ((data[9] | (data[8] << 8)) << 8);

            if (size != data.Length)
            {
                throw new Exception("Data length error");
            }

            var buffer = new byte[size];

            Array.Copy(data, 0, buffer, 0, 16);

            var key = BitConverter.GetBytes(((size + 0x6F349) * ((0x20 * size) ^ 0xA5)) ^ 0x34A9B129);

            for (int i = 16, j = 0; i < data.Length; i++, j++)
            {
                buffer[i] = (byte)(data[i] ^ key[j & 3]);
            }

            return buffer;
        }
    }
}
