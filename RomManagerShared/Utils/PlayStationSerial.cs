using System.Text.RegularExpressions;
using RomManagerShared.Base;

namespace RomManagerShared.Utils;

/// <summary>Region from a PlayStation disc serial prefix (SLUS_…, SCES_…, SLPM_…), as on PS1 and PS2 discs.</summary>
public static partial class PlayStationSerial
{
    [GeneratedRegex("^(SCUS|SLUS)", RegexOptions.IgnoreCase)] private static partial Regex Usa();
    [GeneratedRegex("^(SCES|SLES|SCED)", RegexOptions.IgnoreCase)] private static partial Regex Europe();
    [GeneratedRegex("^(SCPS|SLPS|SLPM|SIPS)", RegexOptions.IgnoreCase)] private static partial Regex Japan();

    public static Region RegionOf(string? serial)
    {
        var name = Path.GetFileName(serial ?? "");
        if (Usa().IsMatch(name)) return Region.USA;
        if (Europe().IsMatch(name)) return Region.Europe;
        return Japan().IsMatch(name) ? Region.Japan : Region.Unknown;
    }
}
