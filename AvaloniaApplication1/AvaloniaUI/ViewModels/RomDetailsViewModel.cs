using Avalonia.Media.Imaging;

namespace AvaloniaUI.ViewModels;

public sealed record DetailRow(string Key, string Value);
/// <summary>A line of a section; <paramref name="Rom"/> is set when it names another rom of the library (clickable).</summary>
public sealed record DetailItem(string Text, Rom? Rom = null)
{
    public bool IsLink => Rom is not null;
}
public sealed record DetailSection(string Header, IReadOnlyList<DetailItem> Items);

/// <summary>
/// Everything known about a rom: the side panel and the Properties dialog show the same thing. Images load in the
/// background; related games (base game, updates, DLC) are links (<see cref="NavigateRequested"/>).
/// </summary>
public sealed partial class RomDetailsViewModel : ViewModelBase, IDisposable
{
    private const int MaxImages = 6;
    private static readonly HashSet<string> SpecialKeys = new(StringComparer.OrdinalIgnoreCase)
        { "Title", "Type", "Region", "Description", "Hashes", "Rating" };

    private readonly CancellationTokenSource _cts = new();

    public RomViewModel Rom { get; }
    public string Title => Rom.Title;
    public string Subtitle => $"{Rom.RomType} · {Rom.Region}";
    public IReadOnlyList<DetailRow> Properties { get; }
    public IReadOnlyList<DetailRow> Hashes { get; }
    public bool HasHashes => Hashes.Count > 0;
    public IReadOnlyList<DetailSection> Sections { get; }
    public string? Description { get; }
    public DetailItem? BaseGame { get; }
    public bool HasBaseGame => BaseGame is not null;

    /// <summary>A related rom was clicked; the page selects it.</summary>
    public event Action<Rom>? NavigateRequested;
    public ObservableCollection<Bitmap> Images { get; } = [];
    public bool HasImages => Images.Count > 0;

    public RomDetailsViewModel(RomViewModel rom, ImageCache? images)
    {
        Rom = rom;
        var src = rom.RomSource;

        Properties = rom.Properties
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value) && kv.Value != "N/A" && !SpecialKeys.Contains(kv.Key))
            .Select(kv => new DetailRow(Labels.Property(kv.Key), kv.Value))
            .ToList();
        Hashes = src.Hashes?.Select(h => new DetailRow(h.Type.ToString(), h.Value ?? "")).ToList() ?? [];
        Description = src.Descriptions?.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.Value))?.Value;

        var sections = new List<DetailSection>();
        if (src.Ratings?.Count > 0)
            sections.Add(new(Labels.Property("Rating"), src.Ratings.Select(r => new DetailItem(
                $"{r.Name}: {r.Age}+" + (r.RatingContents?.Any() == true ? $" ({string.Join(", ", r.RatingContents)})" : ""))).ToList()));
        if (src is Game game)
        {
            AddSection(sections, Strings.RomDetails_Updates, game.Updates?
                .OrderBy(u => VersionKey.Parse(u.Version))
                .Select(u => new DetailItem(Name(u) + (string.IsNullOrEmpty(u.Version) ? "" : $" v{u.Version}"), u)));
            AddSection(sections, Strings.RomDetails_Dlc, game.DLCs?.Select(d => new DetailItem(Name(d), d)));
            AddSection(sections, Strings.RomDetails_RomHacks, game.RomHacks?.Select(h => new DetailItem(Name(h), h)));
        }
        Sections = sections;

        BaseGame = src switch
        {
            Update { RelatedGame: { } g } => new DetailItem(Name(g), g),
            DLC { RelatedGame: { } g } => new DetailItem(Name(g), g),
            _ => null,
        };

        if (images is not null)
            _ = LoadImagesAsync(images, src);
    }

    private static string Name(Rom r) => r.Titles?.FirstOrDefault()?.Value ?? r.TitleID ?? "Unknown";

    private static void AddSection(List<DetailSection> sections, string header, IEnumerable<DetailItem>? items)
    {
        var list = items?.ToList();
        if (list is { Count: > 0 })
            sections.Add(new($"{header} ({list.Count})", list.Take(50).ToList()));
    }

    [RelayCommand]
    private void Open(Rom? rom)
    {
        if (rom is not null) NavigateRequested?.Invoke(rom);
    }

    [RelayCommand]
    private static Task Copy(string? value) => string.IsNullOrEmpty(value) ? Task.CompletedTask : Shell.CopyAsync(value);

    private async Task LoadImagesAsync(ImageCache cache, Rom src)
    {
        IEnumerable<string?> sources = [src.Icon, src.Banner, src.Thumbnail, .. src.Images ?? []];
        foreach (var source in sources.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().Take(MaxImages))
        {
            var path = await cache.GetAsync(source);
            if (path is null || _cts.IsCancellationRequested) continue;
            try
            {
                var bitmap = await Task.Run(() =>
                {
                    using var fs = FileUtils.OpenRead(path);
                    return Bitmap.DecodeToWidth(fs, 360);
                });
                if (_cts.IsCancellationRequested) { bitmap.Dispose(); return; }
                Images.Add(bitmap);
                OnPropertyChanged(nameof(HasImages));
            }
            catch (Exception ex) { AppLog.Warn($"[Image] {source}: {ex.Message}"); }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        foreach (var image in Images) image.Dispose();
        Images.Clear();
    }
}
