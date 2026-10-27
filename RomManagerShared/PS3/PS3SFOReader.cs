using RomManagerShared.Base;
using RomManagerShared.PS3;
using RomManagerShared.Utils;

namespace RomManagerShared.PSVITA;

public static class PS3SFOReader
{
    public static Rom? ParseSFO(MemoryStream stream) => GetData(ParamSfo.Read(stream.ToArray()));

    public static Rom? ParseSFO(byte[] data) => GetData(ParamSfo.Read(data));

    private static Rom? GetData(ParamSfo sfo)
    {
        var category = sfo.Category.ToUpperInvariant();
        Rom? rom = category is "DG" or "HG" or "2G" or "2P" or "1P" or "MN" or "PE"
            ? new PS3Game()
            : category is "GD" or "2D" ? new PS3DLC() : new PS3Update();
        if (rom == null) return null;
        rom.TitleID = sfo.TitleId;
        rom.ProductCode = sfo.ContentId;
        if (sfo.Title is { } title) rom.AddTitleName(title);
        rom.Version = sfo.AppVersion;
        rom.MinimumFirmware = sfo.MinimumFirmware;
        return rom;
    }
}
