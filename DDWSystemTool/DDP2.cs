using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DDWSystemTool
{
    class DDP2
    {
        public static void Extract(string filePath, string outputPath, Action<string> detailOutput)
        {
            using (var reader = new BinaryReader(File.OpenRead(filePath)))
            {
                Extract(reader, outputPath, detailOutput);
            }
        }

        static void Extract(BinaryReader reader, string outputPath, Action<string> detailOutput)
        {
            // "DDP2"
            if (reader.ReadInt32() != 0x32504444)
            {
                throw new Exception("Not valid DDP2 file");
            }

            // Number of entries in the package
            int count = reader.ReadInt32();

            // This value is "sizeof(header) + count * sizeof(entry)"
            reader.ReadInt32();

            // Reserved fields, zero filled
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();

            // Read index

            var entries = new List<Entry>(count);

            for (int i = 0; i < count; i++)
            {
                entries.Add(new Entry
                {
                    Offset = reader.ReadInt32(),
                    Size = reader.ReadInt32(),
                    CompressedSize = reader.ReadInt32(),
                    Flags = reader.ReadInt32()
                });
            }

            // Create output directory

            Directory.CreateDirectory(outputPath);

            // Helper
            byte[] ReadEntry(Entry entry)
            {
                reader.BaseStream.Position = entry.Offset;

                if (entry.CompressedSize != 0)
                {
                    return DDLZ.Decompress(reader.ReadBytes(entry.CompressedSize), entry.Size);
                }

                return reader.ReadBytes(entry.Size);
            }

            // Write entries out

            for (int i = 0; i < count; i++)
            {
                // Detail
                detailOutput?.Invoke($"Extract {i:D8}");

                // Read the data of entry
                var data = ReadEntry(entries[i]);

                // Generate a file name
                var name = Utility.AutoFileNameExtension(data, i.ToString("D8"));

                // Make output file path
                var path = Path.Combine(outputPath, name);

                // Write out
                File.WriteAllBytes(path, data);
            }
        }

        public static void Create(string filePath, string rootPath, Action<string> detailOutput)
        {
            var files = new List<string>();

            // Get file list
            foreach (var path in Directory.EnumerateFiles(rootPath, "*.*"))
            {
                // Ignore empty file
                if (new FileInfo(path).Length == 0)
                    continue;

                files.Add(path);
            }

            // No file
            if (files.Count == 0)
                return;

            // Ensure the order for entries
            files.Sort((a, b) => Path.GetFileNameWithoutExtension(a).CompareTo(Path.GetFileNameWithoutExtension(b)));

            // Create package
            using (var writer = new BinaryWriter(File.Create(filePath)))
            {
                Create(writer, files, detailOutput);
            }
        }

        static void Create(BinaryWriter writer, IList<string> files, Action<string> detailOutput)
        {
            // "DDP2"
            writer.Write(0x32504444);

            // Number of entries in the package
            writer.Write(files.Count);

            // This value is "sizeof(header) + count * sizeof(entry)"
            writer.Write(32 + files.Count * 16);

            // Reserved fields, zero filled
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            // Save index position
            var indexPos = writer.BaseStream.Position;

            // Fill index space
            for (int i = 0; i < files.Count; i++)
            {
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
            }

            var entries = new List<Entry>(files.Count);

            // Write file data to package

            for (int i = 0; i < files.Count; i++)
            {
                var path = files[i];
                var name = Path.GetFileName(path);

                // Detail
                detailOutput?.Invoke($"Add {i:D8} {name}");

                // Read data to memory
                var data = File.ReadAllBytes(path);

                // Save entry information
                entries.Add(new Entry
                {
                    Offset = checked((int)writer.BaseStream.Position),
                    Size = data.Length
                });

                // Write data to package
                writer.Write(data);
            }

            // Write index

            writer.BaseStream.Position = indexPos;

            for (int i = 0; i < files.Count; i++)
            {
                writer.Write(entries[i].Offset);
                writer.Write(entries[i].Size);
                writer.Write(0);
                writer.Write(0);
            }

            // Finish
            writer.Flush();
        }

        public static bool Valid(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            using (var reader = new BinaryReader(stream))
            {
                return reader.ReadUInt32() == 0x32504444;
            }
        }

        struct Entry
        {
            public int Offset;
            public int Size;
            public int CompressedSize;
            public int Flags;
        }
    }
}
