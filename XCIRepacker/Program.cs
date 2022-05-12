using LibHac;
using LibHac.Fs;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LibHac.Common.Keys;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using Path = System.IO.Path;

namespace XCIRepacker
{
    class Program
    {
        static readonly int _bufferSize = 1024 * 1024 * 50;

        static void Main(string[] args)
        {
            Console.Title = "XCIRepacker v0.1 by Ac_K";

            Console.WriteLine(Environment.NewLine +
                              " XCIRepacker v0.1 by Ac_K" +
                              Environment.NewLine +
                              Environment.NewLine +
                              " Provide with the courtesy of the mob." +
                              Environment.NewLine +
                              Environment.NewLine +
                              "---------------------------------------" +
                              Environment.NewLine);

            if (args.Length == 1)
            {
                KeySet keySet = OpenKeySet();

                if (keySet != null)
                {
                    if (File.Exists(args[0]))
                    {
                        try
                        {
                            Xci xciFile = new Xci(keySet, new LocalStorage(args[0], FileAccess.Read));

                            if (xciFile.HasPartition(XciPartitionType.Secure))
                            {
                                XciPartition xciPartition = xciFile.OpenPartition(XciPartitionType.Root);
                                
                                IFile ncaStorage = xciPartition.OpenFile(xciPartition.Files.FirstOrDefault(x=>x.Name == "secure"), OpenMode.Read);
                                
                                var outputPath = Path.Combine(Path.GetDirectoryName(args[0]), $"{Path.GetFileNameWithoutExtension(args[0])}.nsp");

                                Console.WriteLine($" Input  File Path: {args[0]}");
                                Console.WriteLine($" Output File Path: {outputPath}" + Environment.NewLine);

                                using (Stream       dataInput  = ncaStorage.AsStream())
                                using (FileStream   fileOutput = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite))
                                using (BinaryReader reader     = new BinaryReader(fileOutput))
                                using (BinaryWriter writer     = new BinaryWriter(fileOutput))
                                {
                                    int  bytesRead  = -1;
                                    long totalReads = 0;
                                    long totalBytes = dataInput.Length;

                                    byte[] bytes = new byte[_bufferSize];

                                    Console.WriteLine(" Extracting Secure partition..." + Environment.NewLine);

                                    while ((bytesRead = dataInput.Read(bytes, 0, _bufferSize)) > 0)
                                    {
                                        fileOutput.Write(bytes, 0, bytesRead);
                                        totalReads += bytesRead;

                                        DrawTextProgressBar(totalReads, totalBytes);
                                    }

                                    Console.WriteLine(Environment.NewLine + 
                                                      Environment.NewLine +
                                                      " Secure partition extracted!" +
                                                      Environment.NewLine +
                                                      Environment.NewLine +
                                                      "---------------------------------------" +
                                                      Environment.NewLine);

                                    Console.Write(" Patching HFS0 Header to PFS0...");

                                    fileOutput.Position = 0;

                                    string Magic = Encoding.ASCII.GetString(reader.ReadBytes(0x04));

                                    if (Magic == "HFS0")
                                    {
                                        fileOutput.Seek(0x00, SeekOrigin.Begin);

                                        writer.Write(new byte[] { 0x50, 0x46, 0x53, 0x30 }); // PFS0

                                        int filesNumber    = reader.ReadInt32(); // Skip write files number because is at same offset
                                        int filesNamesSize = reader.ReadInt32();

                                        reader.ReadInt32(); // Skip reserved

                                        long HFS0HeaderSize = filesNamesSize + 0x10;

                                        for (int i = 0; i < filesNumber; i++)
                                        {
                                            fileOutput.Seek((i * 0x40) + 0x10, SeekOrigin.Begin);

                                            long fileOffset     = reader.ReadInt64();
                                            long fileSize       = reader.ReadInt64();
                                            int  fileNameOffset = reader.ReadInt32();
                                            int  hashedSize     = reader.ReadInt32();

                                            reader.ReadInt64(); // reserved

                                            reader.ReadBytes(0x20);

                                            HFS0HeaderSize += 0x40;

                                            fileOutput.Seek((i * 0x18) + 0x10, SeekOrigin.Begin);

                                            writer.Write(fileOffset);
                                            writer.Write(fileSize);
                                            writer.Write(fileNameOffset);
                                            writer.Write(0x00); // reserved
                                        }

                                        long endPatchedHeader = fileOutput.Position;

                                        fileOutput.Seek((4 * 0x40) + 0x10, SeekOrigin.Begin);

                                        byte[] filesNamesTable = reader.ReadBytes(filesNamesSize);

                                        fileOutput.Seek(endPatchedHeader, SeekOrigin.Begin);

                                        writer.Write(filesNamesTable);

                                        int shiftSize = (int)(HFS0HeaderSize - fileOutput.Position);

                                        writer.Write(new byte[shiftSize]);

                                        fileOutput.Seek(0x08, SeekOrigin.Begin);

                                        writer.Write(filesNamesSize + shiftSize);

                                        Console.WriteLine(" Done!");
                                    }
                                    else
                                    {
                                        Console.WriteLine(" ERROR: Extracted file isn't HFS0!");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($" ERROR: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine(" ERROR: XCI file not found!");
                    }
                }
                else
                {
                    Console.WriteLine(" ERROR: Keys file not found!");
                }
            }
            else
            {
                Console.WriteLine(" USAGE: XCIRepacker.exe \"PathOfFile.xci\"");
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        public static KeySet OpenKeySet()
        {
            string keyFileName  = "prod.keys";
            string homeKeyFile  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".switch", keyFileName);
            string localKeyFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), keyFileName);

            if (File.Exists(localKeyFile))
            {
                return ExternalKeyReader.ReadKeyFile(localKeyFile);
            }

            if (File.Exists(homeKeyFile))
            {
                return ExternalKeyReader.ReadKeyFile(homeKeyFile);
            }

            return null;
        }

        public static void DrawTextProgressBar(long progress, long total)
        {
            // Modified from https://stackoverflow.com/a/44810285
            int totalChunks = 30;

            Console.CursorLeft = 1;
            Console.Write("[");
            Console.CursorLeft = totalChunks + 2;
            Console.Write("]");
            Console.CursorLeft = 2;

            double pctComplete = Convert.ToDouble(progress) / total;
            int numChunksComplete = Convert.ToInt16(totalChunks * pctComplete);

            Console.BackgroundColor = ConsoleColor.Green;
            Console.Write("".PadRight(numChunksComplete));

            Console.BackgroundColor = ConsoleColor.Gray;
            Console.Write("".PadRight(totalChunks - numChunksComplete));

            Console.CursorLeft = totalChunks + 5;
            Console.BackgroundColor = ConsoleColor.Black;

            string output = (progress / 1024 / 1024).ToString() + " Mb of " + (total / 1024 / 1024).ToString() + " Mb";
            Console.Write(output.PadRight(15) + " extracted!");
        }
    }
}