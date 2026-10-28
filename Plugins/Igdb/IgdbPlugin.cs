using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using RomManagerShared.Base;

[assembly: PluginApiVersion(PluginApi.Version)]

namespace RomManager.Plugins.Igdb;

/// <summary>
/// Metadata from IGDB (https://api-docs.igdb.com): summary, release date, genres, developer, publisher and cover,
/// searched by title on the console's platform. Shows the matches, then saves them to the library with undo.
///
/// Setup: IGDB uses Twitch accounts. Register an application at https://dev.twitch.tv/console/apps (any OAuth
/// redirect, e.g. http://localhost), then enter its Client ID and a Client Secret in Settings → Plugins → IGDB.
/// </summary>
[Plugin("IGDB", "1.0.0", Description = "Fetches summaries, genres, release dates, developers and covers from IGDB",
    Author = "RomManager", Permissions = [PluginPermission.Network, PluginPermission.LibraryMetadata])]
public sealed class IgdbPlugin : IPlugin, IPluginSettings
{
    private const int MaxLookups = 50;

    public string PluginName => "IGDB";
    public string PluginVersion => "1.0.0";
    public string PluginDescription => "Fetches summaries, genres, release dates, developers and covers from IGDB";
    public string PluginAuthor => "RomManager";
    public IReadOnlyList<string> SupportedConsoles => [];
    public IReadOnlyList<PluginPermission> RequiredPermissions => [PluginPermission.Network, PluginPermission.LibraryMetadata];

    /// <summary>IGDB platform ids by console id; consoles not listed are searched on every platform.</summary>
    internal static readonly Dictionary<string, int[]> PlatformIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NES"] = [18, 99], ["SNES"] = [19, 58], ["Nintendo64"] = [4], ["GameCube"] = [21], ["Wii"] = [5],
        ["WiiU"] = [41], ["Switch"] = [130], ["GameBoy"] = [33, 22], ["GameBoyAdvance"] = [24], ["DS"] = [20],
        ["ThreeDS"] = [37], ["PS1"] = [7], ["PS2"] = [8], ["PS3"] = [9], ["PS4"] = [48], ["PSP"] = [38],
        ["PSVita"] = [46], ["Genesis"] = [29], ["MasterSystem"] = [64], ["GameGear"] = [35], ["SegaSaturn"] = [32],
        ["Dreamcast"] = [23], ["PCEngine"] = [86], ["Xbox"] = [11, 12],
    };

    public IReadOnlyList<PluginOption> Options =>
    [
        new PluginOption
        {
            Name = "Fetch metadata from IGDB…",
            Description = $"Look up the selected games (up to {MaxLookups}) on IGDB and save what was found",
            Category = "Metadata",
            CanExecutePredicate = roms => roms.Any(r => r is Game),
        },
        new PluginOption
        {
            Name = "Open on IGDB",
            Description = "Search IGDB for the selected game in the browser",
            Category = "Metadata",
            CanExecutePredicate = roms => roms.Take(2).Count() == 1,
        },
    ];

    public IReadOnlyList<PluginSettingDescriptor> SettingDescriptors =>
    [
        new("ClientId", "Twitch Client ID", PluginSettingKind.Text, Description: "From your application at https://dev.twitch.tv/console/apps"),
        new("ClientSecret", "Twitch Client Secret", PluginSettingKind.Secret, Description: "Generated on the same page"),
        new("ReplaceTitles", "Also replace titles", PluginSettingKind.Boolean, "false",
            "Use IGDB's name as the title (otherwise the title from the file or title database is kept)"),
        new("Covers", "Save covers", PluginSettingKind.Boolean, "true", "Download IGDB's cover art"),
    ];

    private HttpClient? _http;
    private (string Token, DateTime Expires)? _token;

    public Task InitializeAsync(IPluginContext context)
    {
        _http = context.CreateHttpClient();
        return Task.CompletedTask;
    }

    public Task ExecuteAsync(int optionIndex, IPluginContext context, CancellationToken cancellationToken) => optionIndex switch
    {
        0 => FetchMetadataAsync(context, cancellationToken),
        1 => OpenOnIgdbAsync(context),
        _ => Task.CompletedTask,
    };

    private async Task FetchMetadataAsync(IPluginContext context, CancellationToken ct)
    {
        var clientId = context.Settings.GetValueOrDefault("ClientId", "").Trim();
        var secret = context.Settings.GetValueOrDefault("ClientSecret", "").Trim();
        if (clientId.Length == 0 || secret.Length == 0)
        {
            await context.ShowMessageAsync("IGDB", "IGDB needs a Twitch Client ID and Client Secret: register an application at "
                + "https://dev.twitch.tv/console/apps and enter both in Settings → Plugins → IGDB.");
            return;
        }

        var games = context.SelectedRoms.Where(r => r is Game && Title(r) is not null).Take(MaxLookups).ToList();
        var platforms = PlatformIds.GetValueOrDefault(context.ConsoleId);
        var found = new List<(Rom Rom, IgdbGame Match)>();
        try
        {
            var token = await TokenAsync(clientId, secret, ct);
            for (int i = 0; i < games.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                context.Progress.Report(i * 80.0 / games.Count);
                if (await SearchAsync(clientId, token, Title(games[i])!, platforms, ct) is { } match) found.Add((games[i], match));
                await Task.Delay(260, ct); // IGDB allows 4 requests a second
            }
        }
        catch (HttpRequestException ex)
        {
            context.Logger.LogWarning("IGDB request failed: {Message}", ex.Message);
            await context.ShowMessageAsync("IGDB", ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest
                ? "Twitch refused the Client ID or Secret. Check them in Settings → Plugins → IGDB."
                : $"IGDB could not be reached: {ex.Message}");
            _token = null;
            return;
        }

        if (found.Count == 0)
        {
            await context.ShowMessageAsync("IGDB", $"No matches on IGDB for the {games.Count} selected game(s).");
            return;
        }

        await context.ShowTableAsync($"IGDB: {found.Count} of {games.Count} found",
            ["Game", "IGDB match", "Released", "Genres", "Developer"],
            [.. found.Select(f => (IReadOnlyList<string>)
                [Title(f.Rom)!, f.Match.Name, f.Match.ReleaseDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? "",
                 string.Join(", ", f.Match.Genres?.Select(g => g.Name) ?? []), f.Match.Company(developer: true) ?? ""])]);
        if (!await context.ConfirmAsync("IGDB", $"Save IGDB's metadata for {found.Count} game(s) to the library? (Undo is in the Operations panel.)", "Save"))
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
                Description = string.IsNullOrWhiteSpace(match.Summary) ? null : match.Summary,
                Developer = match.Company(developer: true),
                Publisher = match.Company(developer: false),
                ReleaseDate = match.ReleaseDate,
                Genres = match.Genres is { Count: > 0 } g ? [.. g.Select(x => x.Name)] : null,
                CoverImage = covers ? await DownloadCoverAsync(match.Cover?.ImageId, context, ct) : null,
            }, ct);
        }
        context.Progress.Report(100);
    }

    // Twitch app access token (client credentials), kept until shortly before it expires.
    private async Task<string> TokenAsync(string clientId, string secret, CancellationToken ct)
    {
        if (_token is { } t && t.Expires > DateTime.UtcNow) return t.Token;
        using var response = await _http!.PostAsync(
            $"https://id.twitch.tv/oauth2/token?client_id={Uri.EscapeDataString(clientId)}&client_secret={Uri.EscapeDataString(secret)}&grant_type=client_credentials",
            null, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TwitchToken>(ct) ?? throw new HttpRequestException("Empty token response");
        _token = (body.AccessToken, DateTime.UtcNow.AddSeconds(Math.Max(60, body.ExpiresIn - 300)));
        return body.AccessToken;
    }

    private async Task<IgdbGame?> SearchAsync(string clientId, string token, string title, int[]? platforms, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/games")
        {
            Content = new StringContent(Query(title, platforms), Encoding.UTF8, "text/plain"),
        };
        request.Headers.Add("Client-ID", clientId);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var response = await _http!.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var results = await response.Content.ReadFromJsonAsync<List<IgdbGame>>(ct);
        return results?.FirstOrDefault();
    }

    /// <summary>The Apicalypse query for a title, limited to the console's platforms when known.</summary>
    internal static string Query(string title, int[]? platforms)
    {
        var escaped = title.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        var where = platforms is { Length: > 0 } ? $" where platforms = ({string.Join(',', platforms)});" : "";
        return $"search \"{escaped}\"; fields name,summary,first_release_date,genres.name,cover.image_id,"
            + "involved_companies.developer,involved_companies.publisher,involved_companies.company.name;"
            + where + " limit 1;";
    }

    private async Task<byte[]?> DownloadCoverAsync(string? imageId, IPluginContext context, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(imageId)) return null;
        try { return await _http!.GetByteArrayAsync($"https://images.igdb.com/igdb/image/upload/t_cover_big/{imageId}.jpg", ct); }
        catch (HttpRequestException ex)
        {
            context.Logger.LogWarning("IGDB cover download failed: {Message}", ex.Message);
            return null;
        }
    }

    private static Task OpenOnIgdbAsync(IPluginContext context)
    {
        if (context.SelectedRoms.FirstOrDefault() is { } rom && Title(rom) is { } title)
            context.OpenInBrowser(new Uri($"https://www.igdb.com/search?q={Uri.EscapeDataString(title)}"));
        return Task.CompletedTask;
    }

    private static string? Title(Rom rom) => rom.Titles?.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Value))?.Value;

    public ValueTask DisposeAsync()
    {
        _http?.Dispose();
        return ValueTask.CompletedTask;
    }

    // ── IGDB DTOs ─────────────────────────────────────────────────────────

    private sealed class TwitchToken
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; init; } = "";
        [JsonPropertyName("expires_in")] public long ExpiresIn { get; init; }
    }

    internal sealed class IgdbGame
    {
        [JsonPropertyName("name")] public string Name { get; init; } = "";
        [JsonPropertyName("summary")] public string? Summary { get; init; }
        [JsonPropertyName("first_release_date")] public long? FirstReleaseDate { get; init; }
        [JsonPropertyName("genres")] public List<IgdbNamed>? Genres { get; init; }
        [JsonPropertyName("cover")] public IgdbCover? Cover { get; init; }
        [JsonPropertyName("involved_companies")] public List<IgdbInvolvedCompany>? InvolvedCompanies { get; init; }

        public DateOnly? ReleaseDate => FirstReleaseDate is { } s ? DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(s).UtcDateTime) : null;

        public string? Company(bool developer) =>
            InvolvedCompanies?.FirstOrDefault(c => developer ? c.Developer : c.Publisher)?.Company?.Name;
    }

    internal sealed class IgdbNamed
    {
        [JsonPropertyName("name")] public string Name { get; init; } = "";
    }

    internal sealed class IgdbCover
    {
        [JsonPropertyName("image_id")] public string? ImageId { get; init; }
    }

    internal sealed class IgdbInvolvedCompany
    {
        [JsonPropertyName("developer")] public bool Developer { get; init; }
        [JsonPropertyName("publisher")] public bool Publisher { get; init; }
        [JsonPropertyName("company")] public IgdbNamed? Company { get; init; }
    }
}
