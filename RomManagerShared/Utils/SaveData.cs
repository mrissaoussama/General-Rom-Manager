using System.IO.Compression;
using System.Text.Json;
using RomManagerShared.Base;

namespace RomManagerShared.Utils;

/// <summary>A save file or folder found for a rom.</summary>
public sealed record SaveLocation(string Path, bool IsDirectory, string Source);

/// <summary>
/// Finds a game's saves where common emulators keep them, and backs them up to (and restores them from) a zip.
/// Looked at: files next to the rom with the same name (<c>.sav</c>, <c>.srm</c>…: mGBA, melonDS, Snes9x, RetroArch
/// with "saves in content folder"), and per console the folders of Citra/Azahar/Lime3DS, Yuzu-based Switch
/// emulators, PPSSPP, Dolphin, RPCS3 and Vita3K, both portable (next to the emulator) and installed (user profile).
/// Nothing is written while looking.
/// </summary>
public static class SaveData
{
    /// <summary>Extensions of save files kept next to the rom.</summary>
    public static readonly IReadOnlyList<string> SideExtensions = [".sav", ".srm", ".sav2", ".dsv", ".eep", ".sra", ".fla", ".mpk", ".mcr", ".rtc"];

    private const string ManifestName = "romanager-saves.json";

    private sealed record ManifestEntry(string Name, string Path, bool IsDirectory);

    /// <summary>
    /// Existing saves of <paramref name="rom"/>. <paramref name="emulatorPath"/> (Settings → Emulators) adds the
    /// emulator's portable folders; <paramref name="appData"/> and <paramref name="documents"/> default to the
    /// user's (tests pass their own).
    /// </summary>
    public static IReadOnlyList<SaveLocation> Find(string consoleId, Rom rom, string? emulatorPath = null,
        string? appData = null, string? documents = null)
    {
        appData ??= Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        documents ??= Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var emulatorDir = string.IsNullOrWhiteSpace(emulatorPath) ? null : System.IO.Path.GetDirectoryName(emulatorPath);
        var found = new List<SaveLocation>();

        if (rom.Path is { } romPath && !rom.IsFolderFormat)
        {
            var stem = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(romPath) ?? "", System.IO.Path.GetFileNameWithoutExtension(romPath));
            foreach (var ext in SideExtensions)
                if (File.Exists(stem + ext)) found.Add(new SaveLocation(stem + ext, false, "Next to the ROM"));
        }

        var id = rom.TitleID?.Trim();
        if (!string.IsNullOrEmpty(id) && rom is not (Update or DLC))
            foreach (var (path, source) in EmulatorCandidates(consoleId, id, emulatorDir, appData, documents))
                if (Directory.Exists(path)) found.Add(new SaveLocation(path, true, source));
                else if (path.EndsWith('*') && Directory.Exists(System.IO.Path.GetDirectoryName(path)))
                    // A prefix: PSP/PS3 save folders are the serial plus a suffix (ULES00590DATA00).
                    foreach (var dir in Directory.EnumerateDirectories(System.IO.Path.GetDirectoryName(path)!, System.IO.Path.GetFileName(path)))
                        found.Add(new SaveLocation(dir, true, source));

        return [.. found.DistinctBy(f => f.Path, FileUtils.PathComparer)];
    }

    // Folders to look at per console; a trailing '*' is a folder-name prefix.
    private static IEnumerable<(string Path, string Source)> EmulatorCandidates(string consoleId, string id, string? emulatorDir,
        string appData, string documents)
    {
        IEnumerable<string> Roots(string portableSub, params string[] installed) =>
            (emulatorDir is null ? [] : new[] { System.IO.Path.Combine(emulatorDir, portableSub) }).Concat(installed);

        switch (consoleId)
        {
            case "ThreeDS" when id.Length == 16:
                // sdmc/Nintendo 3DS/<id0>/<id1>/title/<high>/<low>/data: emulators use zeroes for id0/id1.
                var zero = new string('0', 32);
                foreach (var root in Roots("user", [.. new[] { "Azahar", "Lime3DS", "Citra" }.Select(n => System.IO.Path.Combine(appData, n))]))
                    yield return (System.IO.Path.Combine(root, "sdmc", "Nintendo 3DS", zero, zero, "title",
                        id[..8].ToLowerInvariant(), id[8..].ToLowerInvariant(), "data"), "Citra/Azahar");
                break;
            case "Switch" when id.Length == 16:
                // nand/user/save/0000000000000000/<user id>/<title id>
                foreach (var root in Roots("user", [.. new[] { "eden", "citron", "sudachi", "suyu", "yuzu" }.Select(n => System.IO.Path.Combine(appData, n))]))
                {
                    var saves = System.IO.Path.Combine(root, "nand", "user", "save", "0000000000000000");
                    if (!Directory.Exists(saves)) continue;
                    foreach (var user in Directory.EnumerateDirectories(saves))
                        yield return (System.IO.Path.Combine(user, id.ToUpperInvariant()), "Yuzu-based emulator");
                }
                break;
            case "PSP":
                foreach (var root in Roots("memstick", System.IO.Path.Combine(documents, "PPSSPP")))
                    yield return (System.IO.Path.Combine(root, "PSP", "SAVEDATA", id.ToUpperInvariant() + "*"), "PPSSPP");
                break;
            case "PS3":
                if (emulatorDir is not null)
                    yield return (System.IO.Path.Combine(emulatorDir, "dev_hdd0", "home", "00000001", "savedata", id.ToUpperInvariant() + "*"), "RPCS3");
                break;
            case "PSVita":
                foreach (var root in Roots("ux0", System.IO.Path.Combine(appData, "Vita3K", "Vita3K", "ux0")))
                    yield return (System.IO.Path.Combine(root, "user", "00", "savedata", id.ToUpperInvariant()), "Vita3K");
                break;
            case "Wii" when id.Length >= 4:
                // Wii/title/00010000/<game id as hex>/data
                var hex = string.Concat(id[..4].Select(c => ((int)c).ToString("x2", System.Globalization.CultureInfo.InvariantCulture)));
                foreach (var root in Roots("User", System.IO.Path.Combine(documents, "Dolphin Emulator"), System.IO.Path.Combine(appData, "Dolphin Emulator")))
                    yield return (System.IO.Path.Combine(root, "Wii", "title", "00010000", hex, "data"), "Dolphin");
                break;
        }
    }

    /// <summary>
    /// Zips <paramref name="saves"/> into <paramref name="zipPath"/> with a manifest of where each came from, so
    /// <see cref="Restore"/> can put them back. An existing zip is replaced.
    /// </summary>
    public static void Backup(IReadOnlyList<SaveLocation> saves, string zipPath)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(zipPath))!);
        var temp = zipPath + ".tmp";
        using (var zip = ZipFile.Open(temp, ZipArchiveMode.Create))
        {
            var manifest = new List<ManifestEntry>();
            for (int i = 0; i < saves.Count; i++)
            {
                var name = $"{i}_{System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(saves[i].Path))}";
                manifest.Add(new ManifestEntry(name, saves[i].Path, saves[i].IsDirectory));
                if (!saves[i].IsDirectory)
                    zip.CreateEntryFromFile(saves[i].Path, name);
                else
                    foreach (var file in Directory.EnumerateFiles(saves[i].Path, "*", SearchOption.AllDirectories))
                        zip.CreateEntryFromFile(file, name + "/" + System.IO.Path.GetRelativePath(saves[i].Path, file).Replace('\\', '/'));
            }
            using var writer = new StreamWriter(zip.CreateEntry(ManifestName).Open());
            writer.Write(JsonSerializer.Serialize(manifest));
        }
        File.Move(temp, zipPath, overwrite: true);
    }

    /// <summary>The places a backup restores to (from its manifest).</summary>
    public static IReadOnlyList<SaveLocation> Contents(string zipPath)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        return [.. ReadManifest(zip).Select(e => new SaveLocation(e.Path, e.IsDirectory, "Backup"))];
    }

    /// <summary>Puts a backup's saves back where they came from; a restored folder replaces what is there.</summary>
    public static void Restore(string zipPath)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        foreach (var entry in ReadManifest(zip))
        {
            if (entry.IsDirectory)
            {
                var prefix = entry.Name + "/";
                if (Directory.Exists(entry.Path)) Directory.Delete(entry.Path, recursive: true);
                Directory.CreateDirectory(entry.Path);
                var root = System.IO.Path.GetFullPath(entry.Path) + System.IO.Path.DirectorySeparatorChar;
                foreach (var file in zip.Entries.Where(e => e.FullName.StartsWith(prefix, StringComparison.Ordinal) && e.Name.Length > 0))
                {
                    var target = System.IO.Path.GetFullPath(System.IO.Path.Combine(entry.Path, file.FullName[prefix.Length..]));
                    if (!target.StartsWith(root, FileUtils.PathComparison)) continue; // no "../" out of the save folder
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target)!);
                    file.ExtractToFile(target, overwrite: true);
                }
            }
            else if (zip.GetEntry(entry.Name) is { } file)
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(entry.Path)!);
                file.ExtractToFile(entry.Path, overwrite: true);
            }
        }
    }

    private static List<ManifestEntry> ReadManifest(ZipArchive zip)
    {
        var entry = zip.GetEntry(ManifestName) ?? throw new InvalidDataException("Not a RomManager save backup (no manifest)");
        using var stream = entry.Open();
        return JsonSerializer.Deserialize<List<ManifestEntry>>(stream) ?? [];
    }
}
