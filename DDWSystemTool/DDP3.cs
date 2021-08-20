using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DDWSystemTool
{
    class DDP3
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
            // "DDP3"
            if (reader.ReadInt32() != 0x33504444)
            {
                throw new Exception("Not valid DDP3 file");
            }

            // Number of buckets of hash table in package
            int bucketCount = reader.ReadInt32();

            // This value is "sizeof(header) + sizeof(index)"
            reader.ReadInt32();

            // Reserved fields, zero filled
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();

            // Read bucket index

            var buckets = new List<Bucket>(bucketCount);

            for (int i = 0; i < bucketCount; i++)
            {
                buckets.Add(new Bucket
                {
                    Hash = reader.ReadInt32(),
                    Offset = reader.ReadInt32()
                });
            }

            // Read bucket items

            var entries = new List<Entry>();

            for (int i = 0; i < buckets.Count; i++)
            {
                // Seek to bucket
                reader.BaseStream.Position = buckets[i].Offset;

                // Read all entries
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var addr = reader.BaseStream.Position;
                    var size = reader.ReadByte();

                    if (size == 0)
                    {
                        // End of bucket
                        break;
                    }

                    // Read entry
                    var entry = new Entry
                    {
                        Offset = reader.ReadInt32(),
                        Size = reader.ReadInt32(),
                        CompressedSize = reader.ReadInt32(),
                        Flags = reader.ReadInt32(),
                        Name = reader.ReadUnicodeString()
                    };

                    // Check for reading
                    if (reader.BaseStream.Position - addr != size)
                    {
                        // Bad read
                        throw new Exception("Bad entry format");
                    }

                    entries.Add(entry);
                }
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

            // Write all entries out

            foreach (var entry in entries)
            {
                // Make file name
                var name = string.Format("{0}@{1:x}", entry.Name, entry.Offset);

                // Detail
                detailOutput?.Invoke($"Extract {name}");

                // Read the data of entry
                var data = ReadEntry(entry);

                // Guess extension
                name = Utility.AutoFileNameExtension(data, name);

                // Make output file path
                var path = Path.Combine(outputPath, name);

                // Write out
                File.WriteAllBytes(path, data);
            }
        }

        public static void Create(string filePath, string rootPath, Action<string> detailOutput)
        {
            var entries = new List<PEntry>();

            // Get file list
            foreach (var path in Directory.EnumerateFiles(rootPath, "*.*"))
            {
                var info = new FileInfo(path);

                // Ignore empty file
                if (info.Length == 0)
                    continue;

                // Grab file name
                var name = Path.GetFileNameWithoutExtension(path);

                // Remove unnecessary parts of the file name
                var iat = name.LastIndexOf('@');
                if (iat != -1)
                    name = name.Substring(0, iat);

                // Name cannot be too long
                if (name.Length > 118)
                    continue;

                // Save entry information
                entries.Add(new PEntry
                {
                    Path = path,
                    Name = name
                });
            }

            // Guess the number of buckets
            var bucketCount = Math.Min(512, Math.Max(32, entries.Count / 5));

            // Create all buckets

            var buckets = new List<PBucket>(bucketCount);

            for (int i = 0; i < bucketCount; i++)
            {
                buckets.Add(new PBucket());
            }

            // Put entries into the bucket

            foreach (var entry in entries)
            {
                // Calc hash of the entry name
                var hash = Hash(entry.Name);
                hash ^= hash >> 11;

                // Calc index of the bucket
                int index = (int)(hash % bucketCount);

                // Put entry into bucket
                buckets[index].Entries.Add(entry);
            }

            // Create package file
            using (var writer = new BinaryWriter(File.Create(filePath)))
            {
                Create(writer, buckets, detailOutput);
            }
        }

        static void Create(BinaryWriter writer, IList<PBucket> buckets, Action<string> detailOutput)
        {
            // "DDP3"
            writer.Write(0x33504444);

            // Number of buckets of hash table in package
            writer.Write(buckets.Count);

            // This value is "sizeof(header) + sizeof(index)"
            writer.Write(0);

            // Reserved fields, zero filled
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            // Write bucket index

            foreach (var bucket in buckets)
            {
                bucket.MyOffset = checked((int)writer.BaseStream.Position);

                writer.Write(0);
                writer.Write(0);
            }

            // Write buckets

            foreach (var bucket in buckets)
            {
                // Save offset of the first entry
                bucket.Offset = checked((int)writer.BaseStream.Position);

                // Write entries
                foreach (var entry in bucket.Entries)
                {
                    // Save offset
                    entry.MyOffset = checked((int)writer.BaseStream.Position);

                    // Calc entry size
                    var size = Convert.ToByte(1 + 16 + (entry.Name.Length + 1) * 2);

                    writer.Write(size);
                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(0);
                    writer.WriteUnicodeString(entry.Name);
                }

                // End of bucket
                writer.Write((byte)0);
            }

            // Save header size
            var headerSize = checked((uint)writer.BaseStream.Length);

            // Write file data

            foreach (var bucket in buckets)
            {
                foreach (var entry in bucket.Entries)
                {
                    detailOutput?.Invoke($"Add {entry.Name}");

                    var data = File.ReadAllBytes(entry.Path);

                    entry.Offset = checked((int)writer.BaseStream.Position);
                    entry.Size = data.Length;

                    writer.Write(data);
                }
            }

            // Write offset

            foreach (var bucket in buckets)
            {
                writer.BaseStream.Position = bucket.MyOffset;

                // Write bucket index
                writer.Write(0x2b2b2b2b);
                writer.Write(bucket.Offset);

                // Write entries
                foreach (var entry in bucket.Entries)
                {
                    writer.BaseStream.Position = entry.MyOffset;

                    writer.BaseStream.Position += 1;
                    writer.Write(entry.Offset);
                    writer.Write(entry.Size);
                }
            }

            // Write header size
            writer.BaseStream.Position = 8;
            writer.Write(headerSize);

            // Finish
            writer.Flush();
        }

        static uint Hash(string input)
        {
            uint hash = 0;
            uint seed = 1;

            for (int i = 0; i < input.Length; i++)
            {
                hash ^= seed * input[i];
                seed += 0x1F3;
            }

            return hash;
        }

        public static bool Valid(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            using (var reader = new BinaryReader(stream))
            {
                return reader.ReadUInt32() == 0x33504444;
            }
        }

        struct Bucket
        {
            public int Hash;
            public int Offset;
        }

        struct Entry
        {
            public int Offset;
            public int Size;
            public int CompressedSize;
            public int Flags;
            public string Name;
        }

        class PBucket
        {
            public int MyOffset;
            public int Offset;
            public IList<PEntry> Entries = new List<PEntry>();
        }

        class PEntry
        {
            public int MyOffset;
            public int Offset;
            public int Size;
            public string Path;
            public string Name;
        }
    }
}
