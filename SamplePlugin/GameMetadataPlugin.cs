using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using RomManagerShared.Base;

[assembly: PluginApiVersion(PluginApi.Version)]

namespace SamplePlugin;

/// <summary>
/// Sample plugin that queries the free RAWG Video Games Database API
/// (https://rawg.io/apidocs) to fetch game metadata for selected ROMs.
///
/// Setup:
///   1. Register for a free RAWG API key at https://rawg.io/login
///   2. Edit the plugin config file at:
///        %AppData%\RomManager\plugins\GameMetadataFetcher\config.json
///      and set:  { "ApiKey": "YOUR_KEY_HERE" }
///   3. Re-launch the app. The option will appear in the ROM context menu.
/// </summary>
[Plugin("GameMetadataFetcher", "1.0.0",
    Description = "Fetches game metadata from the RAWG API",
    Author = "RomManager Sample",
    Permissions = [PluginPermission.Network])] // its config lives in the plugin data folder: no FileSystem needed
public sealed class GameMetadataPlugin : IPlugin
{
    // ── IPlugin identity ──────────────────────────────────────────────────
    public string PluginName        => "GameMetadataFetcher";
    public string PluginVersion     => "1.0.0";
    public string PluginDescription => "Fetches game metadata (genre, rating, etc.) from RAWG.io";
    public string PluginAuthor      => "RomManager Team";

    public IReadOnlyList<string> SupportedConsoles => []; // all consoles

    public IReadOnlyList<PluginPermission> RequiredPermissions => [PluginPermission.Network];

    public IReadOnlyList<PluginOption> Options =>
    [
        new PluginOption
        {
            Name        = "Fetch Metadata from RAWG",
            Description = "Look up the selected ROM(s) on RAWG.io and display their metadata.",
            CanExecutePredicate = roms => roms.Any(),
        },
        new PluginOption
        {
            Name        = "Open RAWG Page in Browser",
            Description = "Open the RAWG.io page for the first selected ROM in your browser.",
            CanExecutePredicate = roms => roms.Take(1).Any(),
        }
    ];

    // ── Private state ─────────────────────────────────────────────────────
    private HttpClient? _http;
    private string _apiKey = string.Empty;
    private ILogger _logger = null!;

    // ── IPlugin lifecycle ─────────────────────────────────────────────────

    public async Task InitializeAsync(IPluginContext context)
    {
        _logger = context.Logger;
        _http = context.CreateHttpClient(); // granted because Network is declared and approved

        // Load config (API key). OpenFile needs no FileSystem permission inside the plugin data folder.
        if (File.Exists(context.ConfigFilePath))
        {
            try
            {
                await using var stream = context.OpenFile(context.ConfigFilePath, FileMode.Open, FileAccess.Read);
                var doc = await System.Text.Json.JsonDocument.ParseAsync(stream);
                _apiKey = doc.RootElement.TryGetProperty("ApiKey", out var v) ? v.GetString() ?? "" : "";
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to read plugin config: {Message}", ex.Message);
            }
        }
        else
        {
            // Write a template config
            await using var stream = context.OpenFile(context.ConfigFilePath, FileMode.Create, FileAccess.Write);
            await stream.WriteAsync("""{ "ApiKey": "YOUR_RAWG_API_KEY_HERE" }"""u8.ToArray());
            _logger.LogInformation("Created template config at {Path}", context.ConfigFilePath);
        }
    }

    public async Task ExecuteAsync(int optionIndex, IPluginContext context)
    {
        switch (optionIndex)
        {
            case 0: await FetchMetadataAsync(context);   break;
            case 1: await OpenRawgPageAsync(context);    break;
        }
    }

    // ── Option 0: Fetch Metadata ──────────────────────────────────────────

    private async Task FetchMetadataAsync(IPluginContext context)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "YOUR_RAWG_API_KEY_HERE")
        {
            await ShowErrorAsync(context, "RAWG API key not set.",
                $"Edit the config file at:\n{context.ConfigFilePath}\nand replace YOUR_RAWG_API_KEY_HERE.");
            return;
        }

        var results = new List<RawgGame>();
        foreach (var rom in context.SelectedRoms.Take(10)) // fetch at most 10
        {
            var title = rom.Titles?.FirstOrDefault()?.Value ?? rom.TitleID ?? "Unknown";
            try
            {
                var url = $"https://api.rawg.io/api/games?key={_apiKey}&search={Uri.EscapeDataString(title)}&page_size=1";
                var response = await _http!.GetFromJsonAsync<RawgSearchResponse>(url);
                if (response?.Results.Count > 0)
                    results.Add(response.Results[0]);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("RAWG fetch failed for '{Title}': {Message}", title, ex.Message);
            }
        }

        // Build a simple result string and push into the UI
        var text = results.Count == 0
            ? "No results found."
            : string.Join("\n\n", results.Select(r =>
                $"Name:     {r.Name}\n" +
                $"Rating:   {r.Rating:F1}/5  ({r.RatingsCount} votes)\n" +
                $"Released: {r.Released}\n" +
                $"Genres:   {string.Join(", ", r.Genres?.Select(g => g.Name) ?? [])}\n" +
                $"RAWG:     https://rawg.io/games/{r.Slug}"));

        await context.ShowPluginUIAsync(BuildResultPanel(text), "RAWG Metadata");
    }

    // ── Option 1: Open RAWG page ──────────────────────────────────────────

    private async Task OpenRawgPageAsync(IPluginContext context)
    {
        var rom   = context.SelectedRoms.First();
        var title = rom.Titles?.FirstOrDefault()?.Value ?? rom.TitleID ?? "Unknown";
        var url   = $"https://rawg.io/games?search={Uri.EscapeDataString(title)}";

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url, UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(context, "Could not open browser.", ex.Message);
        }
    }

    // ── UI helpers ────────────────────────────────────────────────────────

    private static object BuildResultPanel(string text)
    {
        // Return a plain TextBlock wrapped in a ScrollViewer.
        // Avalonia controls live in the host process so we can new them.
        // NOTE: This plugin references Avalonia only indirectly via the
        //       ShowPluginUIAsync callback — which accepts `object`.
        //       To avoid a hard Avalonia dependency in the plugin, we
        //       use `dynamic` or return a simple string here.
        //
        // For production plugins, add <PackageReference Include="Avalonia" /> to
        // the plugin .csproj and build proper controls.
        return text; // Host will wrap this in a TextBlock if it's a string
    }

    private static async Task ShowErrorAsync(IPluginContext ctx, string title, string detail)
    {
        await ctx.ShowPluginUIAsync($"❌ {title}\n\n{detail}", title);
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────

    public ValueTask DisposeAsync()
    {
        _http?.Dispose();
        return ValueTask.CompletedTask;
    }

    // ── RAWG DTOs ─────────────────────────────────────────────────────────

    private sealed class RawgSearchResponse
    {
        [JsonPropertyName("results")]
        public List<RawgGame> Results { get; init; } = [];
    }

    private sealed class RawgGame
    {
        [JsonPropertyName("name")]    public string Name    { get; init; } = "";
        [JsonPropertyName("slug")]    public string Slug    { get; init; } = "";
        [JsonPropertyName("rating")]  public double Rating  { get; init; }
        [JsonPropertyName("ratings_count")] public int RatingsCount { get; init; }
        [JsonPropertyName("released")] public string? Released { get; init; }
        [JsonPropertyName("genres")]  public List<RawgGenre>? Genres { get; init; }
    }

    private sealed class RawgGenre
    {
        [JsonPropertyName("name")] public string Name { get; init; } = "";
    }
}
