using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Switch.Configuration;

namespace RomManagerShared.Switch;

/// <summary>Switch title-database sources and the status of the key files (counts only, never key values).</summary>
public sealed class SwitchOptions : IConsoleOptions
{
    public string Title => "Nintendo Switch";

    public IReadOnlyList<PluginSettingDescriptor> Options =>
    [
        new("TitleDbUrl", "TitleDB URL", Description: "Base URL of the titledb files; empty: config.json (blawar/titledb)"),
        new("VersionsUrl", "versions.json URL", Description: "Empty: config.json"),
        new("TitleDbPath", "TitleDB folder", Description: "Relative to the data folder; empty: config.json"),
    ];

    public void Apply(IReadOnlyDictionary<string, string> values)
    {
        static string? Value(IReadOnlyDictionary<string, string> v, string key) =>
            v.TryGetValue(key, out var s) && !string.IsNullOrWhiteSpace(s) ? s.Trim() : null;
        SwitchConfiguration.TitleDBUrlOverride = Value(values, "TitleDbUrl");
        SwitchConfiguration.VersionsUrlOverride = Value(values, "VersionsUrl");
        SwitchConfiguration.TitleDBPathOverride = Value(values, "TitleDbPath");
    }

    public async Task<IReadOnlyList<ConsoleStatusLine>> GetStatusAsync()
    {
        var status = await Task.Run(SwitchKeyStatus.ReadDefault);
        var lines = new List<ConsoleStatusLine> { new($"Keys folder: {SwitchKeyStatus.DefaultKeysDirectory}", null) };
        var prod = status.ProdKeys;
        if (!prod.Exists) lines.Add(new("prod.keys: Not found (required for NSP/XCI)", false));
        else if (prod.Error is not null) lines.Add(new($"prod.keys: Found, could not be read ({prod.Error})", false));
        else
        {
            var master = status.HighestMasterKey is { } mk
                ? $", highest master_key_{mk:x2}" + (status.FirmwareHint is { } fw ? $" (firmware {fw}+)" : "")
                : ", no master keys";
            lines.Add(new($"prod.keys: Found, {prod.KeyCount} keys{master}", prod.KeyCount > 0));
        }
        var title = status.TitleKeys;
        if (!title.Exists) lines.Add(new("title.keys: Not found (optional)", null));
        else if (title.Error is not null) lines.Add(new($"title.keys: Found, could not be read ({title.Error})", false));
        else lines.Add(new($"title.keys: Found, {title.KeyCount} title keys", true));
        return lines;
    }
}
