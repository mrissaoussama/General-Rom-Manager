using RomManagerShared.Configuration;

namespace RomManagerShared.Wii.Configuration;

/// <summary><c>Consoles:Wii</c> in config.json.</summary>
public sealed class WiiConfigSection
{
    public WiiGameTdbSection GameTDB { get; set; } = new();
    public WiiWadSection Wad { get; set; } = new();
}

public sealed class WiiGameTdbSection
{
    public string Path { get; set; } = "Data/Wii/wiitdb.xml";
    /// <summary>GameTDB's database: a zip holding wiitdb.xml.</summary>
    public string Url { get; set; } = "https://www.gametdb.com/wiitdb.zip";
}

public sealed class WiiWadSection
{
    public string CommonKeyPath { get; set; } = "Data/Wii/common-key.bin";
}

public static class WiiConfiguration
{
    private static WiiConfigSection Section => RomManagerConfiguration.ForConsole<WiiConfigSection>("Wii");

    public static string GetGameTDBPath() => RomManagerConfiguration.UnderBase(Section.GameTDB.Path);

    public static string GetGameTDBUrl() => Section.GameTDB.Url;

    public static string GetWadCommonKeyPath() => RomManagerConfiguration.UnderBase(Section.Wad.CommonKeyPath);
}
