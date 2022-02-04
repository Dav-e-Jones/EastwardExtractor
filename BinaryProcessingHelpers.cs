using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EastwardExtractor;

internal static class BinaryProcessingHelpers
{
    public static string GetString(BinaryReader reader)
    {
        var byteList = new List<byte>();
        while (true)
        {
            var rb = reader.ReadByte();
            if (rb == 0x00) return Encoding.Default.GetString(byteList.ToArray());
            byteList.Add(rb);
        }
    }
}