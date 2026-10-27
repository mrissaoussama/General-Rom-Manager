using RomManagerShared.Base;
using RomManagerShared.Interfaces;

namespace RomManagerShared.PS4;

public sealed class PS4Options : IConsoleOptions
{
    public string Title => "PlayStation 4";

    public IReadOnlyList<PluginSettingDescriptor> Options =>
    [
        new("OnlineUpdateCheck", "Missing updates: ask Sony's update server (online, one request per game)", PluginSettingKind.Boolean, "false"),
    ];

    public void Apply(IReadOnlyDictionary<string, string> values) =>
        PS4UpdateChecker.OnlineAllowed = values.TryGetValue("OnlineUpdateCheck", out var v) && bool.TryParse(v, out var b) && b;
}
