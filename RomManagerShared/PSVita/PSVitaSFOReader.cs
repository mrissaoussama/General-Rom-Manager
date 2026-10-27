using RomManagerShared.Base;
using RomManagerShared.PSVita;
using RomManagerShared.Utils;

namespace RomManagerShared.PSVITA;

public static class PSVitaSFOReader
{
    public static Rom? ParseSFO(MemoryStream stream) => GetData(ParamSfo.Read(stream.ToArray()));

    public static Rom? ParseSFO(byte[] data) => GetData(ParamSfo.Read(data));

    private static Rom? GetData(ParamSfo sfo)
    {
        var category = sfo.Category.ToUpperInvariant();
        Rom? rom = category.Contains("GD") ? new PSVitaGame()
            : category.Contains("GC") || category.Contains("AC") ? new PSVitaDLC()
            : category.Contains("GP") ? new PSVitaUpdate()
            : null;
        if (rom == null) return null;
        rom.TitleID = sfo.TitleId;
        rom.ProductCode = sfo.ContentId;
        if (sfo.Title is { } title) rom.AddTitleName(title);
        rom.Version = sfo.AppVersion;
        rom.MinimumFirmware = sfo.MinimumFirmware;
        return rom;
    }
}
