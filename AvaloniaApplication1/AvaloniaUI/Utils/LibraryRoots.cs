namespace AvaloniaUI.Utils;

/// <summary>
/// ROM folders from config.json (<c>Consoles:&lt;Name&gt;:RomPath</c>) and settings.json (the primary folder in
/// <see cref="Settings.ConsoleLibraryPaths"/> wins over config.json; more in <see cref="Settings.ConsoleExtraLibraryPaths"/>).
/// Read on every call, so changes in Settings apply without a reload step.
/// </summary>
public sealed class LibraryRoots : ILibraryRoots
{
    public IReadOnlyList<string> GetRoots(Type console)
    {
        var id = ConsoleModules.Describe(console).Id;
        var roots = new List<string>();
        if (Settings.Instance.ConsoleLibraryPaths.TryGetValue(id, out var primary) && !string.IsNullOrWhiteSpace(primary))
            roots.Add(primary);
        else if (ConfiguredPath(id) is { } configured)
            roots.Add(configured);
        if (Settings.Instance.ConsoleExtraLibraryPaths.TryGetValue(id, out var extra))
            roots.AddRange(extra.Where(p => !string.IsNullOrWhiteSpace(p)));
        return [.. roots.Select(p => p.Trim()).Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)];
    }

    public IReadOnlyList<Type> ConfiguredConsoles() => [.. ConsoleModules.ConsoleTypes.Where(t => GetRoots(t).Count > 0)];

    private static string? ConfiguredPath(string id)
    {
        try
        {
            var path = RomManagerConfiguration.Configuration?.GetSection("Consoles").GetSection(id)["RomPath"];
            return string.IsNullOrWhiteSpace(path) ? null : path;
        }
        catch { return null; } // config missing/malformed
    }
}
