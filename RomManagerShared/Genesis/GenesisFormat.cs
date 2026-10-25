namespace RomManagerShared.Genesis;

public enum GenesisLayout { Raw, CopierHeader, Smd }

/// <summary>Mega Drive dump layouts: plain, 512-byte copier header, or SMD (header + interleaved 16 KB blocks).</summary>
public static class GenesisFormat
{
    public const int CopierHeaderSize = 512;
    public const int SmdBlockSize = 16384;

    public static bool HasSegaHeader(ReadOnlySpan<byte> rom) =>
        rom.Length >= 0x200 && (rom.Slice(0x100, 4).SequenceEqual("SEGA"u8) || rom.Slice(0x101, 4).SequenceEqual("SEGA"u8));

    /// <summary>Detects the layout from the first bytes of a dump and the full file length.</summary>
    public static GenesisLayout Detect(ReadOnlySpan<byte> data, long fileLength)
    {
        if (fileLength % SmdBlockSize != CopierHeaderSize || data.Length <= CopierHeaderSize || HasSegaHeader(data))
            return GenesisLayout.Raw;
        return HasSegaHeader(data[CopierHeaderSize..]) ? GenesisLayout.CopierHeader : GenesisLayout.Smd;
    }

    /// <summary>Plain ROM image; <paramref name="data"/> may be a prefix of the file.</summary>
    public static byte[] Normalize(byte[] data, GenesisLayout layout) => layout switch
    {
        GenesisLayout.CopierHeader => data[CopierHeaderSize..],
        GenesisLayout.Smd => Deinterleave(data.AsSpan(CopierHeaderSize)),
        _ => data,
    };

    // Each 16 KB block holds the odd bytes in its first half and the even bytes in its second half.
    public static byte[] Deinterleave(ReadOnlySpan<byte> body)
    {
        var output = body.ToArray();
        const int half = SmdBlockSize / 2;
        for (int block = 0; block + SmdBlockSize <= body.Length; block += SmdBlockSize)
        {
            for (int i = 0; i < half; i++)
            {
                output[block + i * 2] = body[block + half + i];
                output[block + i * 2 + 1] = body[block + i];
            }
        }
        return output;
    }
}
