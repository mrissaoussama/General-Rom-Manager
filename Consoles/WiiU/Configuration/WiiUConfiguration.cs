using RomManagerShared.Configuration;

namespace RomManagerShared.WiiU.Configuration;

/// <summary><c>Consoles:WiiU</c> in config.json.</summary>
public sealed class WiiUConfigSection
{
    /// <summary>User-supplied cdecrypt; not shipped with the app.</summary>
    public string CdecryptPath { get; set; } = "Tools/cdecrypt.exe";
    /// <summary>Opt-in: cdecrypt extracts into the ROM folder.</summary>
    public bool AllowCdecrypt { get; set; }
}

public static class WiiUConfiguration
{
    private static WiiUConfigSection Section => RomManagerConfiguration.ForConsole<WiiUConfigSection>("WiiU");

    public static string GetCdecryptPath() => RomManagerConfiguration.UnderBase(Section.CdecryptPath);

    public static bool AllowCdecrypt() => Section.AllowCdecrypt;
}
