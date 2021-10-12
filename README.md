# Eastward Extractor

Simple cli tool to extract resources from the game Eastward in batch fashion.

## Description

Can extract all .g archive files and hmg/pgf packed images. Tool will also generate bitmap headers for all unpacked image files. Currently not planning on adding repacking functionality to this project because it is a very established process using [QuickBMS](http://aluigi.altervista.org/quickbms.htm). This project was predominantly only for making image viewing easier when browsing though game assets.

## Getting Started

### Dependencies

* Build with .NET core 3.0 so it should run on any reasonably recent x64 Windows system

### Installing

* Build it yourself or download from releases and run with cmd

### Options

```
 -v, --verbose      (Default: false) Verbose Output

  -p, --pgfparse     (Default: false) File is PGF/HMG

  -i, --input        Required. Input File. If specifying folder ALSO use -r

  -o, --output       (Default: ) Output File

  -r, --recursive    (Default: false) Input Folder is searched recursively

  --help             Display this help screen.

  --version          Display version information.
```

## License

This project is licensed under the MIT License 
## Acknowledgments

* Packed Image Format by Allen at Zenhax ([post](https://zenhax.com/viewtopic.php?f=7&t=15259))
