using Avalonia.Media.Imaging;

namespace AvaloniaUI.ViewModels;

public sealed record DetailRow(string Key, string Value);
public sealed record DetailSection(string Header, IReadOnlyList<string> Items);

/// <summary>Side panel content for the selected rom; images load in the background.</summary>
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
    public string? BaseGame { get; }
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
            sections.Add(new("Ratings", src.Ratings.Select(r =>
                $"{r.Name}: {r.Age}+" + (r.RatingContents?.Any() == true ? $" ({string.Join(", ", r.RatingContents)})" : "")).ToList()));
        if (src is Game game)
        {
            AddSection(sections, "Updates", game.Updates?.Select(u => Name(u) + (string.IsNullOrEmpty(u.Version) ? "" : $" v{u.Version}")));
            AddSection(sections, "DLC", game.DLCs?.Select(Name));
            AddSection(sections, "ROM hacks", game.RomHacks?.Select(Name));
        }
        Sections = sections;

        BaseGame = src switch
        {
            Update { RelatedGame: { } g } => Name(g),
            DLC { RelatedGame: { } g } => Name(g),
            _ => null,
        };

        if (images is not null)
            _ = LoadImagesAsync(images, src);
    }

    private static string Name(Rom r) => r.Titles?.FirstOrDefault()?.Value ?? r.TitleID ?? "Unknown";

    private static void AddSection(List<DetailSection> sections, string header, IEnumerable<string>? items)
    {
        var list = items?.ToList();
        if (list is { Count: > 0 })
            sections.Add(new($"{header} ({list.Count})", list.Take(20).ToList()));
    }

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
