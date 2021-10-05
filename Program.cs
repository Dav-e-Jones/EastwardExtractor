using CommandLine;
using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ZstdNet;

namespace gdec
{
    class Program
    {

        public class Options
        {
            [Option('v', "verbose", Default = false, HelpText = "Verbose Output")]
            public bool Verbose { get; set; }

            [Option('p', "pgfparse", Default = false, HelpText = "File is PGF/HMG")]
            public bool IsPGF { get; set; }

            [Option('i', "input", Required =true, HelpText ="Input File. If specifying folder ALSO use -r")]
            public string InputFile { get; set; }

            [Option('o', "output", Default = "", HelpText = "Output File")]
            public string OutputFile { get; set; }
            
            [Option('r', "recursive", Default = false, HelpText = "Input Folder is searched recursively")]
            public bool Recursive { get; set; }

        }
        public struct GArchiveFile
        {
            public GArchiveFile(string name, uint startOffset, uint length, bool zipped)
            {
                Name = name;
                SOffset = startOffset;
                Length = length;
                Content = null;
                Zipped = zipped;
            }
            public string Name { get; set; }
            public uint SOffset { get; set; }
            public uint Length { get; set; }
            public byte[] Content { get; set; }
            public bool Zipped { get; set; }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size= 0x6C)]
        [Serializable]
        public struct BmpHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] magic;
            public UInt32 bfSize;

            public UInt16 bfReserved1;
            public UInt16 bfReserved2;

            public UInt32 bfOffBits;

            ///////////////////
            // DIB HEADER
            ///////////////////
            public UInt32 biSize; // 108 bytes

            public Int32 biWidth;
            public Int32 biHeight;

            public UInt16 biPlanes;
            public UInt16 biBitCount;

            public UInt32 biCompression;
            public UInt32 biSizeImage;

            public UInt32 printImgResolution1;
            public UInt32 printImgResolution2;

            public Int32 biColorsInPalette;
            public Int32 biImportantColots;

            //Mask are in Big-Endian
            public UInt32 biRedBitMask;
            public UInt32 biGreenBitMask;
            public UInt32 biBlueBitMask;
            public UInt32 biAlphaBitMask;

            public Int32 biLCSWindowsColorSpace;


            public Int32 biXPelsPerMeter;
            public Int32 biYPelsPerMeter;

            public UInt32 biClrUsed;
            public UInt32 biClrImportant;

        }

        static unsafe byte[] GenerateBmpHeaderBytes(int width, int height, int bpp, int imageSize)
        {
            byte[] hrddata = new byte[0x7A];
            fixed (byte* hdrpnt = hrddata)
            {
                Marshal.StructureToPtr(new BmpHeader
                {
                    magic = new byte[] { 0x42, 0x4D },
                    bfSize = ((uint)(0x7A + imageSize)),
                    bfReserved1 = 0x0,
                    bfReserved2 = 0x0,
                    bfOffBits = 0x7A,
                    biSize = 0x6C,
                    biWidth = width,
                    biHeight = -height,
                    biPlanes = 0x1,
                    biBitCount = (ushort)bpp,
                    biCompression = 0x3,
                    biSizeImage = (uint)imageSize,
                    // biXPelsPerMeter = 0xEC4,
                    //biYPelsPerMeter = 0xEC4,
                    //biXPelsPerMeter = 0x9E,
                    //biYPelsPerMeter = 0x9E,
                    //biXPelsPerMeter = 0x314,
                    //biYPelsPerMeter = 0x314,
                    //biXPelsPerMeter = ppm,
                    //biYPelsPerMeter = ppm,
                    //biColorsInPalette = 0x0,
                    //biImportantColots = 0x0,
                    biRedBitMask = 0x000000FF,
                    biGreenBitMask = 0x0000FF00,
                    biBlueBitMask = 0x00FF0000,
                    biAlphaBitMask = 0xFF000000,
                    // biClrUsed = 0x100,
                    // biClrImportant = 0x100
                }, (IntPtr)hdrpnt, true);
            }
            return hrddata;

        }
        static bool ExtractPGFArchiveToBMP(string targetFile, string outFolder, BinaryReader reader)
        {
            if (Verbose)
                Console.WriteLine($"{targetFile}:");
            byte[] header = reader.ReadBytes(3);
            byte[] magicHeader = { 0x50, 0x47, 0x46 };
            if (header[0] == magicHeader[0] && header[1] == magicHeader[1] && header[2] == magicHeader[2])
            {
                if (Verbose)
                    Console.WriteLine("---- Magic Bytes found");
            }
            else
            {
                if (Verbose)
                    Console.WriteLine("---- Magic Bytes NOT found");
                reader.Close();
                reader.Dispose();
                return false;
            }
            // Big thanks to Allen at https://zenhax.com/viewtopic.php?f=7&t=15259
            int UnkLen1 = reader.ReadByte();
            uint filesize = reader.ReadUInt32();
            reader.ReadBytes(UnkLen1);
            uint zsize = reader.ReadUInt32();
            uint width = reader.ReadUInt32();
            int height = reader.ReadInt32();
            byte bpp = reader.ReadByte();
            int unk = reader.ReadByte();
            int unklen2 = reader.ReadByte();
            int unk2 = reader.ReadByte();
            reader.ReadBytes(unklen2);
            long off = reader.BaseStream.Position;
            long imgsize = width * height * (int)bpp / 8;

            reader.Close();
            reader.Dispose();

            byte[] barr = File.ReadAllBytes(targetFile);
            byte[] outf = new byte[imgsize];

            if (Verbose)
                Console.WriteLine($"---- Zsize: {zsize}, Width: {width}, Height: {height}, Imgsize: {imgsize}");
            try
            {
                LZ4Codec.Decode(barr, (int)off, (int)zsize, outf, 0, (int)imgsize);
            } catch (Exception ex)
            {
                Console.WriteLine($"---- Error while decompressing: {ex.Message}");
                return false;
            }

            byte[] hdrdata = GenerateBmpHeaderBytes((int)width, height, bpp, (int)imgsize);
          
            if (outFolder == "")
                outFolder = Path.GetDirectoryName(targetFile);

            File.WriteAllBytes($"{outFolder}\\{Path.GetFileName(targetFile)}.bmp", hdrdata.Concat(outf).ToArray());
            
            if (Verbose)
                Console.WriteLine($"---- Output BMP file: {outFolder}\\{Path.GetFileName(targetFile)}.bmp");
            return true;
        }
        static bool ExtractGArchive(string targetFile, string outFolder, BinaryReader reader)
        {
            if (Verbose)
                Console.WriteLine($"{targetFile}:");

            byte[] header = reader.ReadBytes(2);
            byte[] magicHeader = { 0x37, 0x6A };
            if (header[0] == magicHeader[0] && header[1] == magicHeader[1])
            {
                if (Verbose)
                    Console.WriteLine("---- Magic Bytes found");
            }
            else
            {
                if (Verbose)
                    Console.WriteLine($"---- Magic Bytes NOT found in: {targetFile}");
                reader.Close();
                reader.Dispose();
                return false;
            }

            reader.ReadInt16(); // empty
            var numOfItems = reader.ReadInt16();
            Console.WriteLine($"---- {numOfItems} files");
            reader.ReadInt16(); // empty
            GArchiveFile[] allFiles = new GArchiveFile[numOfItems];
            for (int fidx = 0; fidx < numOfItems; fidx++)
            {
                var filename = getString(reader);
                uint soffset = reader.ReadUInt32();
                uint val1 = reader.ReadUInt32();
                uint val2 = reader.ReadUInt32();
                uint val3 = reader.ReadUInt32();
                if (Verbose)
                    Console.WriteLine($"---- {filename} at {soffset} .... v1: {val1}, v2: {val2}, v3: {val3}");
                allFiles[fidx] = new GArchiveFile(filename, soffset, val3, val1 != 0);
            }
            reader.Close();
            reader.Dispose();
            byte[] barr = File.ReadAllBytes(targetFile);
            var decomp = new Decompressor();
            if (outFolder == "")
            {
                outFolder = $"{Path.GetDirectoryName(targetFile)}\\{Path.GetFileName(targetFile)}_out";
            }
            string folder = outFolder;
            Directory.CreateDirectory(folder);
            foreach (GArchiveFile emfile in allFiles)
            {
                byte[] content = new byte[emfile.Length];
                Array.Copy(barr, emfile.SOffset, content, 0, emfile.Length);
                try
                {
                    byte[] dcontent;
                    if (emfile.Zipped)
                        dcontent = decomp.Unwrap(content);
                    else
                        dcontent = content;
                    if (emfile.Name.Contains("/"))
                    {
                        var ffname = emfile.Name.Split("/");
                        var fpath = $"{folder}\\";
                        for (int folIdx = 0; folIdx < ffname.Length - 1; folIdx++)
                        {
                            string appendChunk = ffname[folIdx] + "\\";
                            fpath += appendChunk;
                            Directory.CreateDirectory(fpath);
                        }

                        System.IO.File.WriteAllBytes($"{fpath}{ffname[ffname.Length - 1]}", dcontent);
                    }
                    else
                    {
                        System.IO.File.WriteAllBytes($"{folder}\\{emfile.Name}", dcontent);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"---- Failed to extract: {targetFile}. ERR: {ex.Message}");
                    //return false;
                }
            }
            Console.WriteLine($"---- Output files written to: {outFolder}");
            return true;
        }

        static bool ProcessFile(string file, string outpath, bool pgf)
        {
            BinaryReader inread = new BinaryReader(File.Open(file, FileMode.Open));
            if (!pgf)
               return ExtractGArchive(file, outpath, inread);
            else
               return ExtractPGFArchiveToBMP(file, outpath, inread);
        }
        static void ProcessDirectory(string dir, string outdir, bool pgf, bool recursive)
        {
            IEnumerable<string> targetFiles;
            int successfulFiles = 0;
            if (recursive)
                targetFiles = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories);
            else
                targetFiles = Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
            foreach (string file in targetFiles)
            {
                try
                {
                    if (ProcessFile(file, outdir, pgf))
                        successfulFiles+=1;

                }
                catch (Exception ex)
                {
                    if (Verbose)
                    {
                        Console.WriteLine($"---- Failed to extract: {file}");
                        Console.WriteLine($"---- Error: {ex.Message}");
                    }


                }  
            }
            Console.WriteLine($"Extracted {successfulFiles} target files successfully");
        }

        static bool Verbose = false;
        static void InterpretArguments(Options options)
        {
            var targetFile = options.InputFile;
            var outFolder = options.OutputFile;

            Verbose = options.Verbose;

            if (!File.Exists(options.InputFile) && !Directory.Exists(options.InputFile))
            {
                Console.WriteLine($"Specified File/Folder not found: {options.InputFile}");
                return;
            }

            if (isPathFolder(options.InputFile))
                ProcessDirectory(options.InputFile, options.OutputFile, options.IsPGF, options.Recursive);
            else
                ProcessFile(targetFile, outFolder, options.IsPGF);
        }
       

        static void Main(string[] args)
        {
            
            var result = Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options => {
                    InterpretArguments(options);
                })
                .WithNotParsed(errors => { 
                   
                   // Console.WriteLine(errors); 
                });
        }

        // Helper stuff
        static bool isPathFolder(string path)
        {
            FileAttributes targetAttr = File.GetAttributes(path);
            return ((targetAttr & FileAttributes.Directory) == FileAttributes.Directory);

        }

        public static string getString(BinaryReader reader)
        {
            List<byte> bytel = new List<byte>();
            while (true)
            {
                
                byte rb = reader.ReadByte();
                if (rb == 0x00)
                {
                    return Encoding.Default.GetString(bytel.ToArray());
                }
                bytel.Add(rb);
            }

        }
    }
}
