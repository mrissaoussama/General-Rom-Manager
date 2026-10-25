using RomManagerShared.Base;

namespace RomManagerShared.Utils.PKGUtils;

public static class PsnPkgRomMapper
{
    /// <summary>Copies package and PARAM.SFO fields onto <paramref name="rom"/>.</summary>
    public static Rom Fill(Rom rom, PsnPkgInfo info, string path)
    {
        var sfo = info.Sfo;
        rom.TitleID = info.TitleId;
        rom.ProductCode = info.ContentId;
        rom.Path = path;
        rom.Version = sfo?.GetString("APP_VER") ?? sfo?.GetString("VERSION") ?? sfo?.GetString("DISC_VERSION");
        rom.MinimumFirmware = FormatFirmware(sfo?.GetString("PS3_SYSTEM_VER") ?? sfo?.GetString("PSP_SYSTEM_VER"));
        rom.AddTitleName(sfo?.GetString("TITLE") ?? string.Empty);
        rom.AddRegion(GetRegion(info.RegionCode));
        return rom;
    }

    // Content ID prefix: UP=US, EP=EU, JP=JP, HP=Asia, KP=Korea, IP=International.
    public static Region GetRegion(char code) => code switch
    {
        'U' => Region.USA,
        'E' => Region.Europe,
        'J' => Region.Japan,
        'H' => Region.Asia,
        'K' => Region.Korea,
        'I' => Region.World,
        _ => Region.Unknown,
    };

    // "03.5500" -> "3.55"
    private static string? FormatFirmware(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var parts = value.Split('.');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var major)) return value;
        return $"{major}.{parts[1][..Math.Min(2, parts[1].Length)]}";
    }
}
