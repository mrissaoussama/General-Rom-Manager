using RomManagerShared.Base;

namespace RomManagerShared.Utils;

/// <summary>"Put each game in its own folder": a game file and its updates and DLC go into a folder named after it.</summary>
public static class RomGrouping
{
    /// <summary>
    /// Where each file of <paramref name="roms"/> would go: next to the game, in <c>&lt;game title&gt;/</c>. Updates and
    /// DLC follow their linked game (<see cref="RomRelations.Link"/>) even when the game is not selected. Folder-format
    /// roms, files already in their game's folder and files several games share are left alone.
    /// </summary>
    public static IReadOnlyList<(Rom Rom, string Destination)> Plan(IEnumerable<Rom> roms)
    {
        var plan = new List<(Rom, string)>();
        var seen = new HashSet<string>(FileUtils.PathComparer);
        foreach (var rom in roms)
        {
            if (rom.IsFolderFormat || string.IsNullOrEmpty(rom.Path) || !File.Exists(rom.Path)) continue;
            var game = rom switch { Game g => g, Update { RelatedGame: { } g } => g, DLC { RelatedGame: { } g } => g, _ => null };
            if (game?.Path is null || game.IsFolderFormat) continue;
            var title = game.Titles?.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Value))?.Value;
            if (string.IsNullOrWhiteSpace(title)) continue;

            var folderName = FileRenamer.SanitizeFileName(title);
            var gameDir = Path.GetDirectoryName(game.Path)!;
            // The game already sits in its folder: its updates/DLC join it there.
            var target = string.Equals(Path.GetFileName(gameDir), folderName, FileUtils.PathComparison)
                ? gameDir
                : Path.Combine(gameDir, folderName);
            var destination = Path.Combine(target, Path.GetFileName(rom.Path));
            if (string.Equals(destination, rom.Path, FileUtils.PathComparison) || !seen.Add(rom.Path)) continue;
            plan.Add((rom, destination));
        }
        return plan;
    }

    /// <summary>
    /// Moves the planned files (each file once; roms sharing a file all get the new path). A file whose destination
    /// exists is skipped. Returns the number moved and the failures.
    /// </summary>
    public static (int Moved, List<(string Path, string? Error)> Failed) Execute(IReadOnlyList<(Rom Rom, string Destination)> plan,
        IEnumerable<Rom> library, CancellationToken ct = default)
    {
        var failed = new List<(string, string?)>();
        int moved = 0;
        var sharing = library.Where(r => r.Path is not null).GroupBy(r => r.Path!, FileUtils.PathComparer)
            .ToDictionary(g => g.Key, g => g.ToList(), FileUtils.PathComparer);
        foreach (var (rom, destination) in plan)
        {
            ct.ThrowIfCancellationRequested();
            var source = rom.Path!;
            try
            {
                if (File.Exists(destination)) throw new IOException("A file with this name is already there");
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Move(source, destination);
                foreach (var other in sharing.GetValueOrDefault(source) ?? [rom]) other.Path = destination;
                rom.Path = destination;
                moved++;
                AppLog.Info($"[Group] '{source}' → '{destination}'");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                failed.Add((source, ex.Message));
            }
        }
        return (moved, failed);
    }
}
