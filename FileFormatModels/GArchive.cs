using System;
using System.IO;
using ZstdNet;

namespace EastwardExtractor.FileFormatModels;

public struct GArchiveFileRecord
{
    public GArchiveFileRecord(string name, uint startOffset, uint length, bool zipped)
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

    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public byte[] Content { get; set; }
    public bool Zipped { get; set; }
}

internal static class GArchive
{
    internal static bool ExtractGArchive(string targetFile, string outFolder, BinaryReader reader, bool verbose = false)
    {
        if (verbose)
            Console.WriteLine($"{targetFile}:");

        var header = reader.ReadBytes(2);
        byte[] magicHeader = {0x37, 0x6A};
        if (header[0] == magicHeader[0] && header[1] == magicHeader[1])
        {
            if (verbose)
                Console.WriteLine("---- Magic Bytes found");
        }
        else
        {
            if (verbose)
                Console.WriteLine($"---- Magic Bytes NOT found in: {targetFile}");
            reader.Close();
            reader.Dispose();
            return false;
        }

        reader.ReadInt16(); // empty
        var numOfItems = reader.ReadInt16();
        Console.WriteLine($"---- {numOfItems} files");
        reader.ReadInt16(); // empty
        var allFiles = new GArchiveFileRecord[numOfItems];
        for (var fileIdx = 0; fileIdx < numOfItems; fileIdx++)
        {
            var filename = BinaryProcessingHelpers.GetString(reader);
            var startOffset = reader.ReadUInt32();
            var val1 = reader.ReadUInt32();
            var val2 = reader.ReadUInt32();
            var val3 = reader.ReadUInt32();
            if (verbose)
                Console.WriteLine($"---- {filename} at {startOffset} .... v1: {val1}, v2: {val2}, v3: {val3}");
            allFiles[fileIdx] = new GArchiveFileRecord(filename, startOffset, val3, val1 != 0);
        }

        reader.Close();
        reader.Dispose();
        var barr = File.ReadAllBytes(targetFile);
        var decomp = new Decompressor();
        if (outFolder == "") outFolder = $"{Path.GetDirectoryName(targetFile)}\\{Path.GetFileName(targetFile)}_out";
        var folder = outFolder;
        Directory.CreateDirectory(folder);
        foreach (var emfile in allFiles)
        {
            var content = new byte[emfile.Length];
            Array.Copy(barr, emfile.SOffset, content, 0, emfile.Length);
            try
            {
                var dataContent = emfile.Zipped ? decomp.Unwrap(content) : content;
                if (emfile.Name.Contains('/'))
                {
                    var fileName = emfile.Name.Split("/");
                    var filePath = $"{folder}\\";
                    for (var folIdx = 0; folIdx < fileName.Length - 1; folIdx++)
                    {
                        var appendChunk = fileName[folIdx] + "\\";
                        filePath += appendChunk;
                        Directory.CreateDirectory(filePath);
                    }

                    File.WriteAllBytes($"{filePath}{fileName[^1]}", dataContent);
                }
                else
                {
                    File.WriteAllBytes($"{folder}\\{emfile.Name}", dataContent);
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
}