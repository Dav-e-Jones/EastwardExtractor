using System;
using System.IO;
using CommandLine;
using EastwardExtractor.FileFormatModels;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace EastwardExtractor;

internal static class Program
{
    private static bool _verbose;

    private static void Main(string[] args)
    {
        // ReSharper disable once UnusedVariable
        var result = Parser.Default.ParseArguments<Options>(args)
            .WithParsed(InterpretArguments)
            .WithNotParsed(errors =>
            {
                // Console.WriteLine(errors); 
            });
    }

    private static void InterpretArguments(Options options)
    {
        var targetFile = options.InputFile;
        var outFolder = options.OutputFile;

        _verbose = options.Verbose;

        if (!File.Exists(options.InputFile) && !Directory.Exists(options.InputFile))
        {
            Console.WriteLine($"Specified File/Folder not found: {options.InputFile}");
            return;
        }

        if (IsPathFolder(options.InputFile))
            ProcessDirectory(options.InputFile, options.OutputFile, options.IsPgf, options.Recursive);
        else
            ProcessFile(targetFile, outFolder, options.IsPgf);
    }

    private static bool ProcessFile(string file, string outPath, bool pgf)
    {
        var inRead = new BinaryReader(File.Open(file, FileMode.Open));
        if (!pgf)
            return GArchive.ExtractGArchive(file, outPath, inRead, _verbose);
        return PgfArchive.ExtractPgfArchiveToBmp(file, outPath, inRead, _verbose);
    }

    private static void ProcessDirectory(string dir, string outDir, bool pgf, bool recursive)
    {
        var successfulFiles = 0;
        var targetFiles = Directory.EnumerateFiles(dir, "*.*",
            recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        foreach (var file in targetFiles)
            try
            {
                if (ProcessFile(file, outDir, pgf))
                    successfulFiles += 1;
            }
            catch (Exception ex)
            {
                if (_verbose)
                {
                    Console.WriteLine($"---- Failed to extract: {file}");
                    Console.WriteLine($"---- Error: {ex.Message}");
                }
            }

        Console.WriteLine($"Extracted {successfulFiles} target files successfully");
    }


    private static bool IsPathFolder(string path)
    {
        var targetAttr = File.GetAttributes(path);
        return (targetAttr & FileAttributes.Directory) == FileAttributes.Directory;
    }

    private class Options
    {
        [Option('v', "verbose", Default = false, HelpText = "Verbose Output")]
        public bool Verbose { get; set; }

        [Option('p', "pgfparse", Default = false, HelpText = "File is PGF/HMG")]
        public bool IsPgf { get; set; }

        [Option('i', "input", Required = true, HelpText = "Input File. If specifying folder ALSO use -r")]
        public string InputFile { get; set; }

        [Option('o', "output", Default = "", HelpText = "Output File")]
        public string OutputFile { get; set; }

        [Option('r', "recursive", Default = false, HelpText = "Input Folder is searched recursively")]
        public bool Recursive { get; set; }
    }
}