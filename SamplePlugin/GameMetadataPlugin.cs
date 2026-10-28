using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using RomManagerShared.Base;

[assembly: PluginApiVersion(PluginApi.Version)]

namespace SamplePlugin;

/// <summary>
/// Looks up the selected roms on the RAWG video game database (https://rawg.io/apidocs), shows what it found and,
/// after the user confirms, saves the description, publisher, developer, release date, genres and cover to the
/// library. Undo in the Operations panel puts the old values back.
///
/// Setup: a free RAWG API key (https://rawg.io/login) in Settings → Plugins → GameMetadataFetcher.
/// This is also the reference plugin: it uses settings, progress, cancellation, tables, confirmation, metadata
/// writes and a library hook.
/// </summary>
[Plugin("GameMetadataFetcher", "2.0.0",
    Description = "Fetches descriptions, genres, release dates and covers from RAWG.io",
    Author = "RomManager",
    Permissions = [PluginPermission.Network, PluginPermission.LibraryMetadata])]
public sealed class GameMetadataPlugin : IPlugin, ILibraryHooks, IPluginSettings
{
    private const int MaxLookups = 50;

    public string PluginName => "GameMetadataFetcher";
    public string PluginVersion => "2.0.0";
    public string PluginDescription => "Fetches descriptions, genres, release dates and covers from RAWG.io";
    public string PluginAuthor => "RomManager";

    public IReadOnlyList<string> SupportedConsoles => []; // all consoles

    public IReadOnlyList<PluginPermission> RequiredPermissions => [PluginPermission.Network, PluginPermission.LibraryMetadata];

    public IReadOnlyList<PluginOption> Options =>
    [
        new PluginOption
        {
            Name = "Fetch metadata from RAWG…",
            Description = $"Look up the selected games (up to {MaxLookups}) on RAWG.io and save what was found",
            Category = "Metadata",
            CanExecutePredicate = roms => roms.Any(r => r is Game),
        },
        new PluginOption
        {
            Name = "Open on RAWG",
            Description = "Search RAWG.io for the selected game in the browser",
            Category = "Metadata",
            CanExecutePredicate = roms => roms.Take(2).Count() == 1,
        },
    ];

    public IReadOnlyList<PluginSettingDescriptor> SettingDescriptors =>
    [
        new("ApiKey", "RAWG API key", PluginSettingKind.Secret, Description: "Free key from https://rawg.io/apidocs"),
        new("ReplaceTitles", "Also replace titles", PluginSettingKind.Boolean, "false",
            "Use RAWG's name as the title (otherwise the title from the file or title database is kept)"),
        new("Covers", "Save covers", PluginSettingKind.Boolean, "true", "Download RAWG's picture as the cover"),
    ];

    private HttpClient? _http;

    public Task InitializeAsync(IPluginContext context)
    {
        _http = context.CreateHttpClient(); // granted because Network is declared and approved
        return Task.CompletedTask;
    }

    // ILibraryHooks: called after every scan, and when files are renamed, moved or deleted.
    public Task OnScanCompletedAsync(LibraryScanCompleted scan, IPluginContext context)
    {
        if (scan.Added.Count > 0 || scan.Removed.Count > 0)
            context.Logger.LogInformation("{Console}: {Added} rom(s) added, {Removed} removed",
                scan.Console, scan.Added.Count, scan.Removed.Count);
        return Task.CompletedTask;
    }

    public Task ExecuteAsync(int optionIndex, IPluginContext context, CancellationToken cancellationToken) => optionIndex switch
    {
        0 => FetchMetadataAsync(context, cancellationToken),
        1 => OpenOnRawgAsync(context),
        _ => Task.CompletedTask,
    };

    private async Task FetchMetadataAsync(IPluginContext context, CancellationToken ct)
    {
        var apiKey = context.Settings.GetValueOrDefault("ApiKey", "");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            await context.ShowMessageAsync("RAWG", "No RAWG API key yet. Enter it in Settings → Plugins → GameMetadataFetcher.");
            return;
        }

        var games = context.SelectedRoms.Where(r => r is Game && Title(r) is not null).Take(MaxLookups).ToList();
        var found = new List<(Rom Rom, RawgGame Match)>();
        for (int i = 0; i < games.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            context.Progress.Report(i * 80.0 / games.Count);
            try
            {
                if (await LookUpAsync(apiKey, Title(games[i])!, ct) is { } match) found.Add((games[i], match));
            }
            catch (HttpRequestException ex)
            {
                context.Logger.LogWarning("RAWG lookup failed for '{Title}': {Message}", Title(games[i]), ex.Message);
                if (ex.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
                {
                    await context.ShowMessageAsync("RAWG", "RAWG refused the API key. Check it in Settings → Plugins → GameMetadataFetcher.");
                    return;
                }
            }
        }

        if (found.Count == 0)
        {
            await context.ShowMessageAsync("RAWG", $"No matches on RAWG for the {games.Count} selected game(s).");
            return;
        }

        await context.ShowTableAsync($"RAWG: {found.Count} of {games.Count} found",
            ["Game", "RAWG match", "Released", "Genres", "Developer"],
            [.. found.Select(f => (IReadOnlyList<string>)
                [Title(f.Rom)!, f.Match.Name, f.Match.Released ?? "", Genres(f.Match), string.Join(", ", f.Match.Developers?.Select(d => d.Name) ?? [])])]);
        if (!await context.ConfirmAsync("RAWG", $"Save RAWG's metadata for {found.Count} game(s) to the library? (Undo is in the Operations panel.)", "Save"))
            return;

        var replaceTitles = context.Settings.GetValueOrDefault("ReplaceTitles") == "true";
        var covers = context.Settings.GetValueOrDefault("Covers", "true") == "true";
        for (int i = 0; i < found.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            context.Progress.Report(80 + i * 20.0 / found.Count);
            var (rom, match) = found[i];
            await context.UpdateMetadataAsync(rom, new RomMetadata
            {
                Title = replaceTitles ? match.Name : null,
                Description = string.IsNullOrWhiteSpace(match.DescriptionRaw) ? null : match.DescriptionRaw,
                Developer = match.Developers?.FirstOrDefault()?.Name,
                Publisher = match.Publishers?.FirstOrDefault()?.Name,
                ReleaseDate = DateOnly.TryParse(match.Released, System.Globalization.CultureInfo.InvariantCulture, out var date) ? date : null,
                Genres = match.GenreList is { Count: > 0 } g ? [.. g.Select(x => x.Name)] : null,
                CoverImage = covers ? await DownloadCoverAsync(match.BackgroundImage, context, ct) : null,
            }, ct);
        }
        context.Progress.Report(100);
    }

    // Search, then the details call for the best hit (search results have no description, developers or publishers).
    private async Task<RawgGame?> LookUpAsync(string apiKey, string title, CancellationToken ct)
    {
        var key = Uri.EscapeDataString(apiKey);
        var search = await _http!.GetFromJsonAsync<RawgSearchResponse>(
            $"https://api.rawg.io/api/games?key={key}&search={Uri.EscapeDataString(title)}&page_size=1", ct);
        if (search?.Results.FirstOrDefault() is not { } hit) return null;
        var details = await _http!.GetFromJsonAsync<RawgGame>($"https://api.rawg.io/api/games/{hit.Id}?key={key}", ct);
        return details ?? hit;
    }

    private async Task<byte[]?> DownloadCoverAsync(string? url, IPluginContext context, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(url)) return null;
        // RAWG serves resized copies: 640 px wide is plenty for a cover card.
        url = url.Replace("/media/games/", "/media/resize/640/-/games/", StringComparison.Ordinal);
        try { return await _http!.GetByteArrayAsync(url, ct); }
        catch (HttpRequestException ex)
        {
            context.Logger.LogWarning("RAWG cover download failed: {Message}", ex.Message);
            return null;
        }
    }

    private static Task OpenOnRawgAsync(IPluginContext context)
    {
        if (context.SelectedRoms.FirstOrDefault() is { } rom && Title(rom) is { } title)
            context.OpenInBrowser(new Uri($"https://rawg.io/search?query={Uri.EscapeDataString(title)}"));
        return Task.CompletedTask;
    }

    private static string? Title(Rom rom) => rom.Titles?.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Value))?.Value;

    private static string Genres(RawgGame game) => string.Join(", ", game.GenreList?.Select(g => g.Name) ?? []);

    public ValueTask DisposeAsync()
    {
        _http?.Dispose();
        return ValueTask.CompletedTask;
    }

    // ── RAWG DTOs ─────────────────────────────────────────────────────────

    private sealed class RawgSearchResponse
    {
        [JsonPropertyName("results")] public List<RawgGame> Results { get; init; } = [];
    }

    private sealed class RawgGame
    {
        [JsonPropertyName("id")] public int Id { get; init; }
        [JsonPropertyName("name")] public string Name { get; init; } = "";
        [JsonPropertyName("released")] public string? Released { get; init; }
        [JsonPropertyName("background_image")] public string? BackgroundImage { get; init; }
        [JsonPropertyName("description_raw")] public string? DescriptionRaw { get; init; }
        [JsonPropertyName("genres")] public List<RawgNamed>? GenreList { get; init; }
        [JsonPropertyName("developers")] public List<RawgNamed>? Developers { get; init; }
        [JsonPropertyName("publishers")] public List<RawgNamed>? Publishers { get; init; }
    }

    private sealed class RawgNamed
    {
        [JsonPropertyName("name")] public string Name { get; init; } = "";
    }
}
