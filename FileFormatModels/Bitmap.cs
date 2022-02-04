using System;
using System.Runtime.InteropServices;

namespace EastwardExtractor.FileFormatModels;

internal static class Bitmap
{
    internal static unsafe byte[] GenerateBmpHeaderBytes(int width, int height, int bpp, int imageSize)
    {
        var headerData = new byte[0x7A];
        fixed (byte* headerPointer = headerData)
        {
            Marshal.StructureToPtr(new BmpHeader
            {
                magic = new byte[] {0x42, 0x4D},
                bfSize = (uint) (0x7A + imageSize),
                bfReserved1 = 0x0,
                bfReserved2 = 0x0,
                bfOffBits = 0x7A,
                biSize = 0x6C,
                biWidth = width,
                biHeight = -height,
                biPlanes = 0x1,
                biBitCount = (ushort) bpp,
                biCompression = 0x3,
                biSizeImage = (uint) imageSize,
                biRedBitMask = 0x000000FF,
                biGreenBitMask = 0x0000FF00,
                biBlueBitMask = 0x00FF0000,
                biAlphaBitMask = 0xFF000000
            }, (IntPtr) headerPointer, true);
        }

        return headerData;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 0x6C)]
    [Serializable]
    public struct BmpHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] magic;

        public uint bfSize;

        public ushort bfReserved1;
        public ushort bfReserved2;

        public uint bfOffBits;

        public uint biSize; // 108 bytes

        public int biWidth;
        public int biHeight;

        public ushort biPlanes;
        public ushort biBitCount;

        public uint biCompression;
        public uint biSizeImage;

        public uint printImgResolution1;
        public uint printImgResolution2;

        public int biColorsInPalette;
        public int biImportantColors;

        //Mask are in Big-Endian
        public uint biRedBitMask;
        public uint biGreenBitMask;
        public uint biBlueBitMask;
        public uint biAlphaBitMask;

        public int biLCSWindowsColorSpace;


        public int biXPixelsPerMeter;
        public int biYPixelsPerMeter;

        public uint biClrUsed;
        public uint biClrImportant;
    }
}