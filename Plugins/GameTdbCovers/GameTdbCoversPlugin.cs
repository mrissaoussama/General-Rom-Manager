using System.Net;
using Microsoft.Extensions.Logging;
using RomManagerShared.Base;

[assembly: PluginApiVersion(PluginApi.Version)]

namespace RomManager.Plugins.GameTdbCovers;

/// <summary>
/// Box art from GameTDB (https://www.gametdb.com) for Nintendo and PS3 games, found by the game's ID (Wii/GameCube
/// game ID, the DS/3DS/Wii U/Switch product code, the PS3 serial). No account needed. Covers are saved to the
/// library with undo.
/// </summary>
[Plugin("GameTDB", "1.0.0", Description = "Box art from GameTDB for Wii, GameCube, Wii U, DS, 3DS, Switch and PS3 games",
    Author = "RomManager", Permissions = [PluginPermission.Network, PluginPermission.LibraryMetadata])]
public sealed class GameTdbCoversPlugin : IPlugin
{
    private const int MaxLookups = 200;

    public string PluginName => "GameTDB";
    public string PluginVersion => "1.0.0";
    public string PluginDescription => "Box art from GameTDB for Wii, GameCube, Wii U, DS, 3DS, Switch and PS3 games";
    public string PluginAuthor => "RomManager";
    public IReadOnlyList<string> SupportedConsoles => [.. Platforms.Keys];
    public IReadOnlyList<PluginPermission> RequiredPermissions => [PluginPermission.Network, PluginPermission.LibraryMetadata];

    /// <summary>GameTDB's folder and image type for each console id.</summary>
    internal static readonly Dictionary<string, (string Folder, string Extension)> Platforms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Wii"] = ("wii", "png"),
        ["GameCube"] = ("wii", "png"), // GameTDB keeps GameCube covers with the Wii ones
        ["WiiU"] = ("wiiu", "jpg"),
        ["DS"] = ("ds", "jpg"),
        ["ThreeDS"] = ("3ds", "jpg"),
        ["Switch"] = ("switch", "jpg"),
        ["PS3"] = ("ps3", "jpg"),
    };

    public IReadOnlyList<PluginOption> Options =>
    [
        new PluginOption
        {
            Name = "Download box art from GameTDB",
            Description = $"Find the selected games' covers on GameTDB (up to {MaxLookups}) and save them to the library",
            Category = "Metadata",
            CanExecutePredicate = roms => roms.Any(r => r is Game),
        },
    ];

    private HttpClient? _http;

    public Task InitializeAsync(IPluginContext context)
    {
        _http = context.CreateHttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("RomManager");
        return Task.CompletedTask;
    }

    public async Task ExecuteAsync(int optionIndex, IPluginContext context, CancellationToken cancellationToken)
    {
        if (optionIndex != 0) return;
        if (!Platforms.TryGetValue(context.ConsoleId, out var platform))
        {
            await context.ShowMessageAsync("GameTDB", $"GameTDB has no box art for {context.ConsoleName}.");
            return;
        }
        var games = context.SelectedRoms.OfType<Game>().Take(MaxLookups).ToList();
        var found = new List<(Rom Rom, Uri Url)>();
        for (int i = 0; i < games.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.Progress.Report(i * 60.0 / games.Count);
            foreach (var url in CandidateUrls(games[i], platform.Folder, platform.Extension))
            {
                try
                {
                    using var head = await _http!.SendAsync(new HttpRequestMessage(HttpMethod.Head, url), cancellationToken);
                    if (head.StatusCode != HttpStatusCode.OK) continue;
                    found.Add((games[i], url));
                    break;
                }
                catch (HttpRequestException ex)
                {
                    context.Logger.LogWarning("GameTDB: {Url}: {Message}", url, ex.Message);
                    break;
                }
            }
        }
        if (found.Count == 0)
        {
            await context.ShowMessageAsync("GameTDB", $"GameTDB has no cover for the {games.Count} selected game(s), or their IDs are unknown.");
            return;
        }
        if (!await context.ConfirmAsync("GameTDB",
                $"Found covers for {found.Count} of {games.Count} game(s). Save them to the library? (Undo is in the Operations panel.)", "Save"))
            return;

        for (int i = 0; i < found.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.Progress.Report(60 + i * 40.0 / found.Count);
            try
            {
                var bytes = await _http!.GetByteArrayAsync(found[i].Url, cancellationToken);
                await context.UpdateMetadataAsync(found[i].Rom, new RomMetadata { CoverImage = bytes }, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                context.Logger.LogWarning("GameTDB: {Url}: {Message}", found[i].Url, ex.Message);
            }
        }
        context.Progress.Report(100);
    }

    /// <summary>
    /// The URLs to try for a game, most likely first: each ID it may go by (title ID, the code at the end of its
    /// product code; Wii U codes with Nintendo's "01" maker code) in the region its code names, then US, EN and JA.
    /// </summary>
    internal static IEnumerable<Uri> CandidateUrls(Rom rom, string folder, string extension)
    {
        var ids = new List<string>();
        void Add(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            id = id.Trim().ToUpperInvariant();
            if (id.Length is >= 4 and <= 10 && id.All(char.IsAsciiLetterOrDigit) && !ids.Contains(id)) ids.Add(id);
        }
        if (folder == "wiiu")
            Add(LastSegment(rom.ProductCode) is { Length: 4 } code ? code + "01" : null);
        Add(rom.TitleID is { Length: <= 10 } ? rom.TitleID : null);
        Add(LastSegment(rom.ProductCode));

        foreach (var id in ids)
            foreach (var region in new[] { RegionOf(id, folder), "US", "EN", "JA" }.Distinct())
                yield return new Uri($"https://art.gametdb.com/{folder}/cover/{region}/{id}.{extension}");
    }

    private static string? LastSegment(string? code) => code?.Split('-', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

    // The region letter: 4th character of Nintendo codes ("RMGE" → E), "BLUS"/"BLES"/"BLJM" for PS3 serials.
    internal static string RegionOf(string id, string folder)
    {
        if (folder == "ps3" && id.Length >= 3)
            return id[2] switch { 'U' => "US", 'E' => "EN", 'J' => "JA", 'K' => "KO", _ => "US" };
        var letter = folder == "switch" ? id[^1] : id.Length >= 4 ? id[3] : ' ';
        return letter switch
        {
            'E' => "US",
            'J' => "JA",
            'K' => "KO",
            'D' => "DE",
            'F' => "FR",
            'S' => "ES",
            'I' => "IT",
            'H' => "NL",
            'R' => "RU",
            'C' or 'W' => "ZH",
            _ => "EN",
        };
    }

    public ValueTask DisposeAsync()
    {
        _http?.Dispose();
        return ValueTask.CompletedTask;
    }
}
