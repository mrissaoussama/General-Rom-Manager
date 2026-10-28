using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using RomManagerShared.Base;

[assembly: PluginApiVersion(PluginApi.Version)]

namespace RomManager.Plugins.Playlists;

/// <summary>
/// Exports the selected games as a RetroArch playlist (<c>.lpl</c>) or an EmulationStation <c>gamelist.xml</c>.
/// Writes only the file the user picks, so it needs no permission.
/// </summary>
[Plugin("Playlists", "1.0.0", Description = "Export the selected games as a RetroArch playlist or an EmulationStation gamelist", Author = "RomManager")]
public sealed class PlaylistsPlugin : IPlugin
{
    public string PluginName => "Playlists";
    public string PluginVersion => "1.0.0";
    public string PluginDescription => "Export the selected games as a RetroArch playlist or an EmulationStation gamelist";
    public string PluginAuthor => "RomManager";
    public IReadOnlyList<string> SupportedConsoles => [];
    public IReadOnlyList<PluginPermission> RequiredPermissions => [];

    public IReadOnlyList<PluginOption> Options =>
    [
        new PluginOption
        {
            Name = "Export RetroArch playlist (.lpl)…",
            Description = "A playlist RetroArch shows under Playlists; cores are detected when a game starts",
            Category = "Export",
            CanExecutePredicate = roms => roms.Any(IsPlayable),
        },
        new PluginOption
        {
            Name = "Export EmulationStation gamelist.xml…",
            Description = "Names, descriptions, dates and covers for EmulationStation / ES-DE (save it in the ROM folder)",
            Category = "Export",
            CanExecutePredicate = roms => roms.Any(IsPlayable),
        },
    ];

    public Task InitializeAsync(IPluginContext context) => Task.CompletedTask;

    public async Task ExecuteAsync(int optionIndex, IPluginContext context, CancellationToken cancellationToken)
    {
        var games = Playable(context.SelectedRoms);
        if (games.Count == 0) return;
        var name = context.ConsoleName.Length > 0 ? context.ConsoleName : "RomManager";
        var (title, file, extension) = optionIndex == 0
            ? ("Save RetroArch playlist", $"{name}.lpl", "lpl")
            : ("Save gamelist.xml", "gamelist.xml", "xml");
        if (await context.PickSaveFileAsync(title, file, extension) is not { } path) return;
        cancellationToken.ThrowIfCancellationRequested();

        var text = optionIndex == 0
            ? RetroArchPlaylist(games, Path.GetFileName(path))
            : EmulationStationGamelist(games, Path.GetDirectoryName(path) ?? "");
        await using (var stream = context.OpenFile(path, FileMode.Create, FileAccess.Write))
        await using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            await writer.WriteAsync(text.AsMemory(), cancellationToken);
        await context.ShowMessageAsync(PluginName, $"{games.Count} game(s) written to {path}");
    }

    // Base games only (updates and DLC are not started on their own); a file holding several roms appears once.
    internal static bool IsPlayable(Rom rom) => rom is Game && !string.IsNullOrEmpty(rom.Path);

    internal static List<Rom> Playable(IEnumerable<Rom> roms) =>
        [.. roms.Where(IsPlayable).DistinctBy(r => r.Path, OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase)
            .OrderBy(Label, StringComparer.CurrentCultureIgnoreCase)];

    internal static string Label(Rom rom) =>
        rom.Titles?.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Value))?.Value?.Trim() ?? Path.GetFileNameWithoutExtension(rom.Path) ?? "";

    /// <summary>RetroArch's JSON playlist format (version 1.5); cores and CRCs left to RetroArch ("DETECT").</summary>
    internal static string RetroArchPlaylist(IEnumerable<Rom> games, string playlistFileName)
    {
        var items = new JsonArray();
        foreach (var rom in games)
        {
            var crc = rom.Hashes?.LastOrDefault(h => h.Type == HashTypeEnum.CRC32)?.Value;
            items.Add(new JsonObject
            {
                ["path"] = rom.Path,
                ["label"] = Label(rom),
                ["core_path"] = "DETECT",
                ["core_name"] = "DETECT",
                ["crc32"] = crc is null ? "DETECT" : $"{crc.ToUpperInvariant()}|crc",
                ["db_name"] = playlistFileName,
            });
        }
        var playlist = new JsonObject
        {
            ["version"] = "1.5",
            ["default_core_path"] = "",
            ["default_core_name"] = "",
            ["label_display_mode"] = 0,
            ["right_thumbnail_mode"] = 0,
            ["left_thumbnail_mode"] = 0,
            ["sort_mode"] = 0,
            ["items"] = items,
        };
        return playlist.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// An EmulationStation gamelist. Paths under <paramref name="folder"/> (where the file is saved) are written as
    /// <c>./relative</c>, as EmulationStation expects; others stay absolute.
    /// </summary>
    internal static string EmulationStationGamelist(IEnumerable<Rom> games, string folder)
    {
        var list = new XElement("gameList");
        foreach (var rom in games)
        {
            var game = new XElement("game",
                new XElement("path", RelativeTo(folder, rom.Path!)),
                new XElement("name", Label(rom)));
            if (rom.Descriptions?.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.Value))?.Value is { } desc) game.Add(new XElement("desc", desc));
            if (Image(rom) is { } image) game.Add(new XElement("image", RelativeTo(folder, image)));
            if (rom.ReleaseDate is { } date) game.Add(new XElement("releasedate", date.ToString("yyyyMMdd'T'000000", CultureInfo.InvariantCulture)));
            if (!string.IsNullOrWhiteSpace(rom.Developer)) game.Add(new XElement("developer", rom.Developer));
            if (!string.IsNullOrWhiteSpace(rom.Publisher)) game.Add(new XElement("publisher", rom.Publisher));
            if (rom.Genres is { Count: > 0 } genres) game.Add(new XElement("genre", string.Join(", ", genres)));
            if (rom.NumberOfPlayers > 0) game.Add(new XElement("players", rom.NumberOfPlayers.ToString(CultureInfo.InvariantCulture)));
            list.Add(game);
        }
        return new XDocument(new XDeclaration("1.0", "utf-8", null), list).Declaration + Environment.NewLine + list;
    }

    // A local picture file (covers saved by plugins, extracted icons); URLs are left out.
    private static string? Image(Rom rom) =>
        new[] { rom.Icon, rom.Thumbnail, rom.Banner }.FirstOrDefault(p => !string.IsNullOrEmpty(p) && Path.IsPathRooted(p) && File.Exists(p));

    internal static string RelativeTo(string folder, string path)
    {
        if (folder.Length == 0) return path;
        var relative = Path.GetRelativePath(folder, path);
        return relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)
            ? path
            : "./" + relative.Replace('\\', '/');
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
