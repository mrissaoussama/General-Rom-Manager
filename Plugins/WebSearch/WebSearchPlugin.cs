using RomManagerShared.Base;

[assembly: PluginApiVersion(PluginApi.Version)]

namespace RomManager.Plugins.WebSearch;

/// <summary>Opens a web search for the selected games: the configured search engine, YouTube or GameFAQs.</summary>
[Plugin("WebSearch", "1.0.0", Description = "Search the web, YouTube or GameFAQs for the selected games", Author = "RomManager")]
public sealed class WebSearchPlugin : IPlugin, IPluginSettings
{
    /// <summary>More tabs than this ask first.</summary>
    internal const int TabsWithoutAsking = 5;

    public string PluginName => "WebSearch";
    public string PluginVersion => "1.0.0";
    public string PluginDescription => "Search the web, YouTube or GameFAQs for the selected games";
    public string PluginAuthor => "RomManager";
    public IReadOnlyList<string> SupportedConsoles => [];
    public IReadOnlyList<PluginPermission> RequiredPermissions => [];

    // {query} is the game's title and console, URL-encoded.
    internal static readonly (string Name, string Description, string? Url)[] Searches =
    [
        ("Search the web", "Search for the game with the search engine from Settings → Plugins → WebSearch", null),
        ("Search YouTube (gameplay)", "Gameplay videos on YouTube", "https://www.youtube.com/results?search_query={query}+gameplay"),
        ("Search GameFAQs", "Guides and FAQs on GameFAQs", "https://gamefaqs.gamespot.com/search?game={query}"),
    ];

    public IReadOnlyList<PluginOption> Options =>
    [
        .. Searches.Select(s => new PluginOption
        {
            Name = s.Name,
            Description = s.Description,
            Category = "Search",
            CanExecutePredicate = roms => roms.Any(r => Title(r) is not null),
        }),
    ];

    public IReadOnlyList<PluginSettingDescriptor> SettingDescriptors =>
    [
        new("SearchUrl", "Search engine", PluginSettingKind.Text, "https://duckduckgo.com/?q={query}",
            "Address of a search, with {query} where the search words go (e.g. https://www.google.com/search?q={query})"),
    ];

    public Task InitializeAsync(IPluginContext context) => Task.CompletedTask;

    public async Task ExecuteAsync(int optionIndex, IPluginContext context, CancellationToken cancellationToken)
    {
        if (optionIndex < 0 || optionIndex >= Searches.Length) return;
        var template = Searches[optionIndex].Url ?? context.Settings.GetValueOrDefault("SearchUrl", "");
        var queries = context.SelectedRoms.Select(r => Query(r, context.ConsoleName)).OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (queries.Count == 0) return;
        if (queries.Count > TabsWithoutAsking
            && !await context.ConfirmAsync(PluginName, $"Open {queries.Count} browser tabs?", "Open"))
            return;
        foreach (var query in queries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (BuildUrl(template, query) is { } url) context.OpenInBrowser(url);
            else
            {
                await context.ShowMessageAsync(PluginName, $"The search engine address is not a valid http(s) address with {{query}}: {template}");
                return;
            }
        }
    }

    /// <summary>The search address for <paramref name="query"/>; null when the template is not an http(s) address.</summary>
    internal static Uri? BuildUrl(string template, string query)
    {
        if (!template.Contains("{query}", StringComparison.Ordinal)) return null;
        var text = template.Replace("{query}", Uri.EscapeDataString(query), StringComparison.Ordinal);
        return Uri.TryCreate(text, UriKind.Absolute, out var url) && (url.Scheme == Uri.UriSchemeHttps || url.Scheme == Uri.UriSchemeHttp) ? url : null;
    }

    /// <summary>"Title Console", or null for a rom without a title.</summary>
    internal static string? Query(Rom rom, string console) =>
        Title(rom) is { } title ? (console.Length > 0 ? $"{title} {console}" : title) : null;

    private static string? Title(Rom rom) => rom.Titles?.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Value))?.Value?.Trim();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
