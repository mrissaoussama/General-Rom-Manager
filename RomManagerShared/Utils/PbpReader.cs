using System.Buffers.Binary;
using RomManagerShared.Configuration;

namespace RomManagerShared.Utils;

/// <summary>
/// PSP EBOOT/PARAM .pbp: "\0PBP", version, then 8 u32 offsets (PARAM.SFO, ICON0.PNG, ICON1.PMF, PIC0.PNG,
/// PIC1.PNG, SND0.AT3, DATA.PSP, DATA.PSAR); each section ends where the next begins.
/// </summary>
public static class PbpReader
{
    public const uint Magic = 0x50425000;
    private const int MaxSection = 16 * 1024 * 1024;

    public sealed record Pbp(ParamSfo? Sfo, byte[]? Icon0);

    public static Pbp? Read(Stream stream)
    {
        Span<byte> header = stackalloc byte[0x28];
        stream.Position = 0;
        if (stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) < header.Length) return null;
        if (BinaryPrimitives.ReadUInt32LittleEndian(header) != Magic) return null;

        var offsets = new uint[9];
        for (int i = 0; i < 8; i++) offsets[i] = BinaryPrimitives.ReadUInt32LittleEndian(header[(8 + i * 4)..]);
        offsets[8] = (uint)Math.Min(stream.Length, uint.MaxValue);

        byte[]? Section(int i)
        {
            long start = offsets[i], end = offsets[i + 1];
            if (end <= start || end > stream.Length || end - start > MaxSection) return null;
            var data = new byte[end - start];
            stream.Position = start;
            stream.ReadExactly(data);
            return data;
        }

        var sfoBytes = Section(0);
        var sfo = sfoBytes is null ? null : ParamSfo.Read(sfoBytes);
        return new Pbp(sfo, Section(1));
    }
}

/// <summary>Writes console artwork pulled out of rom files to Cache/Icons/&lt;console&gt;/ so the UI can show it.</summary>
public static class IconCache
{
    public static string? Save(string console, string? key, byte[]? png)
    {
        if (png is not { Length: > 8 } || string.IsNullOrWhiteSpace(key)) return null;
        var cache = RomManagerConfiguration.GetCachePath();
        if (string.IsNullOrEmpty(cache)) return null;
        try
        {
            var dir = Path.Combine(cache, "Icons", console);
            Directory.CreateDirectory(dir);
            var safe = string.Concat(key.Split(Path.GetInvalidFileNameChars()));
            var path = Path.Combine(dir, safe + ".png");
            if (!File.Exists(path) || new FileInfo(path).Length != png.Length)
                File.WriteAllBytes(path, png);
            return path;
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[Icon] {console}/{key}: {ex.Message}");
            return null;
        }
    }
}
