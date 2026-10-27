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
///   2. Enter it in Settings → Plugins → GameMetadataFetcher (the plugin declares it via IPluginSettings).
///   3. The options appear in the ROM context menu.
/// </summary>
[Plugin("GameMetadataFetcher", "1.0.0",
    Description = "Fetches game metadata from the RAWG API",
    Author = "RomManager Sample",
    Permissions = [PluginPermission.Network])]
public sealed class GameMetadataPlugin : IPlugin, ILibraryHooks, IPluginSettings
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
    private ILogger _logger = null!;

    // ── IPlugin lifecycle ─────────────────────────────────────────────────

    // IPluginSettings: the API key is edited in Settings → Plugins (hidden while typing).
    public IReadOnlyList<PluginSettingDescriptor> SettingDescriptors =>
    [
        new("ApiKey", "RAWG API key", PluginSettingKind.Secret, Description: "Free key from https://rawg.io/apidocs"),
    ];

    public Task InitializeAsync(IPluginContext context)
    {
        _logger = context.Logger;
        _http = context.CreateHttpClient(); // granted because Network is declared and approved
        return Task.CompletedTask;
    }

    // ILibraryHooks: called after every scan of a console this plugin supports.
    public Task OnScanCompletedAsync(LibraryScanCompleted scan, IPluginContext context)
    {
        if (scan.Added.Count > 0 || scan.Removed.Count > 0)
            context.Logger.LogInformation("{Console}: {Added} rom(s) added, {Removed} removed",
                scan.Console, scan.Added.Count, scan.Removed.Count);
        return Task.CompletedTask;
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
        var apiKey = context.Settings.GetValueOrDefault("ApiKey", "");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            await ShowErrorAsync(context, "RAWG API key not set.", "Enter it in Settings → Plugins → GameMetadataFetcher.");
            return;
        }

        var results = new List<RawgGame>();
        foreach (var rom in context.SelectedRoms.Take(10)) // fetch at most 10
        {
            var title = rom.Titles?.FirstOrDefault()?.Value ?? rom.TitleID ?? "Unknown";
            try
            {
                var url = $"https://api.rawg.io/api/games?key={Uri.EscapeDataString(apiKey)}&search={Uri.EscapeDataString(title)}&page_size=1";
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
