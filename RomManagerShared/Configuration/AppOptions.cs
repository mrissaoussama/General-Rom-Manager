namespace RomManagerShared.Configuration;

/// <summary>
/// config.json (+ config.local.json) as typed options. Every value has a default here, so a missing key or file
/// means "use the default", never null. Console modules keep their own section under <c>Consoles:&lt;Id&gt;</c>
/// and read it with <see cref="RomManagerConfiguration.ForConsole{T}"/>.
/// </summary>
public sealed class AppOptions
{
    public GlobalPathsOptions GlobalPaths { get; set; } = new();
    public DatOptions Dats { get; set; } = new();
    /// <summary>Conversion tool paths by tool id, relative to the base folder (<c>Tools:chdman</c>).</summary>
    public Dictionary<string, string> Tools { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Folders and files under the base folder. Relative values are joined to <see cref="BaseDirectory"/>.</summary>
public sealed class GlobalPathsOptions
{
    /// <summary>Data folder; empty = <c>%AppData%/GeneralRomManager</c>. Environment variables are expanded.</summary>
    public string BaseDirectory { get; set; } = "";
    public string Plugins { get; set; } = "Plugins";
    public string Dats { get; set; } = "Data/DAT";
    public string Database { get; set; } = "Data/roms.db";
    public string ErrorLogs { get; set; } = "Logs/errors.log";
    public string Cache { get; set; } = "Cache";
    /// <summary>Extra folder scanned for console module DLLs (besides the app's own Consoles folder).</summary>
    public string Modules { get; set; } = "Consoles";
}

public sealed class DatOptions
{
    /// <summary>Hash disc images for Redump matching (slow); the user setting overrides it.</summary>
    public bool HashDiscImages { get; set; }
}
