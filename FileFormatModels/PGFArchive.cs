using System;
using System.IO;
using System.Linq;
using K4os.Compression.LZ4;

namespace EastwardExtractor.FileFormatModels;

internal static class PgfArchive
{
    internal static bool ExtractPgfArchiveToBmp(string targetFile, string outFolder, BinaryReader reader,
        bool verbose = false)
    {
        if (verbose)
            Console.WriteLine($"{targetFile}:");
        var header = reader.ReadBytes(3);
        byte[] magicHeader = {0x50, 0x47, 0x46};
        if (header[0] == magicHeader[0] && header[1] == magicHeader[1] && header[2] == magicHeader[2])
        {
            if (verbose)
                Console.WriteLine("---- Magic Bytes found");
        }
        else
        {
            if (verbose)
                Console.WriteLine("---- Magic Bytes NOT found");
            reader.Close();
            reader.Dispose();
            return false;
        }

        // Big thanks to Allen at https://zenhax.com/viewtopic.php?f=7&t=15259
        int unkLen1 = reader.ReadByte();
        var filesize = reader.ReadUInt32();
        reader.ReadBytes(unkLen1);
        var zippedSize = reader.ReadUInt32();
        var width = reader.ReadUInt32();
        var height = reader.ReadInt32();
        var bpp = reader.ReadByte();
        int unk = reader.ReadByte();
        int unkLen2 = reader.ReadByte();
        int unk2 = reader.ReadByte();
        reader.ReadBytes(unkLen2);
        var off = reader.BaseStream.Position;
        var imageSize = width * height * bpp / 8;

        reader.Close();
        reader.Dispose();

        var barr = File.ReadAllBytes(targetFile);
        var outFile = new byte[imageSize];

        if (verbose)
            Console.WriteLine(
                $"---- Zipped Size: {zippedSize}, Width: {width}, Height: {height}, Img-size: {imageSize}");
        try
        {
            LZ4Codec.Decode(barr, (int) off, (int) zippedSize, outFile, 0, (int) imageSize);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"---- Error while decompressing: {ex.Message}");
            return false;
        }

        var headerData = Bitmap.GenerateBmpHeaderBytes((int) width, height, bpp, (int) imageSize);

        if (string.IsNullOrEmpty(outFolder))
            outFolder = Path.GetDirectoryName(targetFile);

        File.WriteAllBytes($"{outFolder}\\{Path.GetFileName(targetFile)}.bmp", headerData.Concat(outFile).ToArray());

        if (verbose)
            Console.WriteLine($"---- Output BMP file: {outFolder}\\{Path.GetFileName(targetFile)}.bmp");
        return true;
    }
}