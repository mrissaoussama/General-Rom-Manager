using System.Buffers.Binary;
using System.Text;

namespace RomManagerTests;

/// <summary>Builds PARAM.SFO bytes for tests.</summary>
internal static class SfoTestData
{
    public static byte[] Build(params (string Key, object Value)[] entries)
    {
        var keys = new MemoryStream();
        var values = new MemoryStream();
        var index = new List<(ushort KeyOffset, ushort Format, int Length, int Max, int Offset)>();
        foreach (var (key, value) in entries)
        {
            var keyOffset = (ushort)keys.Length;
            keys.Write(Encoding.ASCII.GetBytes(key + "\0"));
            byte[] data;
            ushort format;
            if (value is int i)
            {
                data = new byte[4];
                BinaryPrimitives.WriteInt32LittleEndian(data, i);
                format = 0x0404;
            }
            else
            {
                data = Encoding.UTF8.GetBytes((string)value + "\0");
                format = 0x0204;
            }
            var max = (data.Length + 3) & ~3;
            index.Add((keyOffset, format, data.Length, max, (int)values.Length));
            values.Write(data);
            values.Write(new byte[max - data.Length]);
        }
        while (keys.Length % 4 != 0) keys.WriteByte(0);

        var keyTable = 0x14 + index.Count * 0x10;
        var dataTable = keyTable + (int)keys.Length;
        var result = new byte[dataTable + values.Length];
        var span = result.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(span, 0x46535000);
        BinaryPrimitives.WriteUInt32LittleEndian(span[0x04..], 0x0101);
        BinaryPrimitives.WriteUInt32LittleEndian(span[0x08..], (uint)keyTable);
        BinaryPrimitives.WriteUInt32LittleEndian(span[0x0C..], (uint)dataTable);
        BinaryPrimitives.WriteUInt32LittleEndian(span[0x10..], (uint)index.Count);
        for (int n = 0; n < index.Count; n++)
        {
            var e = span.Slice(0x14 + n * 0x10, 0x10);
            BinaryPrimitives.WriteUInt16LittleEndian(e, index[n].KeyOffset);
            BinaryPrimitives.WriteUInt16LittleEndian(e[0x02..], index[n].Format);
            BinaryPrimitives.WriteUInt32LittleEndian(e[0x04..], (uint)index[n].Length);
            BinaryPrimitives.WriteUInt32LittleEndian(e[0x08..], (uint)index[n].Max);
            BinaryPrimitives.WriteUInt32LittleEndian(e[0x0C..], (uint)index[n].Offset);
        }
        keys.ToArray().CopyTo(result, keyTable);
        values.ToArray().CopyTo(result, dataTable);
        return result;
    }
}
