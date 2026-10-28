namespace AvaloniaUI.Utils;

/// <summary>
/// ROM folders from config.json (<c>Consoles:&lt;Name&gt;:RomPath</c>) and settings.json (the primary folder in
/// <see cref="Settings.ConsoleLibraryPaths"/> wins over config.json; more in <see cref="Settings.ConsoleExtraLibraryPaths"/>).
/// Each <see cref="Settings.LibraryFolders"/> adds its console sub-folder when it exists. Read on every call, so
/// changes in Settings apply without a reload step.
/// </summary>
public sealed class LibraryRoots : ILibraryRoots
{
    public IReadOnlyList<string> GetRoots(Type console)
    {
        var id = ConsoleModules.Describe(console).Id;
        var roots = new List<string>();
        if (Settings.Instance.ConsoleLibraryPaths.TryGetValue(id, out var primary) && !string.IsNullOrWhiteSpace(primary))
            roots.Add(primary);
        else if (RomManagerConfiguration.GetConsoleRomPath(id) is { } configured)
            roots.Add(configured);
        if (Settings.Instance.ConsoleExtraLibraryPaths.TryGetValue(id, out var extra))
            roots.AddRange(extra.Where(p => !string.IsNullOrWhiteSpace(p)));
        roots.AddRange(ManagedFolders(console).Where(Directory.Exists));
        return [.. roots.Select(p => p.Trim()).Distinct(FileUtils.PathComparer)];
    }

    /// <summary>The console's sub-folder in every library folder (existing or not).</summary>
    public static IReadOnlyList<string> ManagedFolders(Type console)
    {
        var name = LibraryImport.ConsoleFolderName(ConsoleModules.Describe(console));
        return [.. Settings.Instance.LibraryFolders.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => Path.Combine(f.Trim(), name))];
    }

    public IReadOnlyList<Type> ConfiguredConsoles() => [.. ConsoleModules.ConsoleTypes.Where(t => GetRoots(t).Count > 0)];

}
