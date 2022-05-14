using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DDWSystemTool
{
    class Program
    {
        public static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (args.Length != 2)
            {
                Console.WriteLine("DDWSystemTool");
                Console.WriteLine("Usage:");
                Console.WriteLine("  Extract DDP2 or DDP3 : DDWSystemTool -x [file|folder]");
                Console.WriteLine("  Create DDP2 file     : DDWSystemTool -2 [folder]");
                Console.WriteLine("  Create DDP3 file     : DDWSystemTool -3 [folder]");
                Console.WriteLine("  Export script text   : DDWSystemTool -e [file|folder]");
                Console.WriteLine("  Export all text      : DDWSystemTool -a [file|folder]");
                Console.WriteLine("  Rebuild script       : DDWSystemTool -b [file|folder]");
                Console.WriteLine("  Disassemble          : DDWSystemTool -d [file|folder]");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            string mode = args[0];
            string path = Path.GetFullPath(args[1]);

            switch (mode)
            {
                case "-x":
                {
                    void Extract(string filePath)
                    {
                        Console.WriteLine($"Extracting {filePath}");

                        if (DDP2.Valid(filePath))
                        {
                            var outputPath = Path.ChangeExtension(filePath, "");
                            DDP2.Extract(filePath, outputPath, null);
                        }
                        else if (DDP3.Valid(filePath))
                        {
                            var outputPath = Path.ChangeExtension(filePath, "");
                            DDP3.Extract(filePath, outputPath, null);
                        }
                        else
                        {
                            Console.WriteLine("Not a 'DDP2' or 'DDP3' file.");
                            return;
                        }
                    }

                    if (Utility.PathIsFolder(path))
                    {
                        foreach (var item in Directory.EnumerateFiles(path, "*.dat"))
                        {
                            Extract(item);
                        }
                    }
                    else
                    {
                        Extract(path);
                    }

                    break;
                }
                case "-2":
                case "-3":
                {
                    int version = mode == "-2" ? 2 : 3;

                    void CreatePackage(string rootPath)
                    {
                        var filePath = rootPath + ".dat";

                        if (File.Exists(filePath))
                            filePath = Path.ChangeExtension(filePath, "new.dat");

                        Console.WriteLine($"Creating package {filePath}");

                        if (version == 2)
                        {
                            DDP2.Create(filePath, rootPath, msg => Console.WriteLine(msg));
                            return;
                        }
                        if (version == 3)
                        {
                            DDP3.Create(filePath, rootPath, null);
                            return;
                        }
                    }

                    CreatePackage(path);

                    break;
                }
                case "-e":
                case "-a":
                {
                    bool exportAll = (mode == "-a");

                    void ExportString(string filePath)
                    {
                        Console.WriteLine($"Exporting text from {Path.GetFileName(filePath)}");

                        try
                        {
                            var script = new HXB.Binary();
                            script.Load(filePath);

                            if (exportAll)
                                script.ExportAllStrings(Path.ChangeExtension(filePath, "txt"));
                            else
                                script.ExportStrings(Path.ChangeExtension(filePath, "txt"));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }

                    if (Utility.PathIsFolder(path))
                    {
                        foreach (var item in Directory.EnumerateFiles(path, "*.hxb"))
                        {
                            ExportString(item);
                        }
                    }
                    else
                    {
                        ExportString(path);
                    }

                    break;
                }
                case "-b":
                {
                    void RebuildScript(string filePath)
                    {
                        Console.WriteLine($"Rebuilding script {Path.GetFileName(filePath)}");

                        try
                        {
                            string textFilePath = Path.ChangeExtension(filePath, "txt");
                            string newFilePath = Path.GetDirectoryName(filePath) + @"\rebuild\" + Path.GetFileName(filePath);
                            Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));
                            var script = new HXB.Binary();
                            script.Load(filePath);
                            script.ImportStrings(textFilePath);
                            script.Save(newFilePath);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }

                    if (Utility.PathIsFolder(path))
                    {
                        foreach (var item in Directory.EnumerateFiles(path, "*.hxb"))
                        {
                            RebuildScript(item);
                        }
                    }
                    else
                    {
                        RebuildScript(path);
                    }

                    break;
                }
                case "-d":
                {
                    void Disasm(string filePath)
                    {
                        Console.WriteLine($"Disassembling {Path.GetFileName(filePath)}");

                        try
                        {
                            var script = new HXB.Binary();
                            script.Load(filePath);
                            script.Disasm(Path.ChangeExtension(filePath, ".asm.txt"));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }

                    if (Utility.PathIsFolder(path))
                    {
                        foreach (var item in Directory.EnumerateFiles(path, "*.hxb"))
                        {
                            Disasm(item);
                        }
                    }
                    else
                    {
                        Disasm(path);
                    }

                    break;
                }
            }
        }
    }
}
