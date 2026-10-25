// csharp - AvaloniaApplication1/AvaloniaUI/ViewModels/RomViewModel.cs

using RomManagerShared.Switch;

namespace AvaloniaUI.ViewModels;

public partial class RomViewModel : ViewModelBase
{
    private readonly Rom _rom;

    /// <summary>Exposes the underlying domain model for backend operations.</summary>
    public Rom RomSource => _rom;

    public string TitleID => _rom.TitleID ?? string.Empty;
    public string Title   => _rom.Titles?.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Value))?.Value ?? "Unknown";
    public string Region  => _rom.Regions?.Any() == true ? string.Join(", ", _rom.Regions) : "Unknown";
    public string Version => _rom.Version ?? "N/A";
    public string RomType => DetermineRomType();
    public string Path => _rom.Path ?? string.Empty;

    /// <summary>Group header text when the list is grouped by game.</summary>
    public string GroupKey { get; set; } = "";
    /// <summary>Owning console name (shown in the all-consoles view).</summary>
    public string Console { get; init; } = "";

    /// <summary>Shared cache for card covers; set at startup.</summary>
    public static ImageCache? CoverCache { get; set; }
    private Avalonia.Media.Imaging.Bitmap? _cover;
    private bool _coverRequested;

    /// <summary>Card cover, loaded on first access (i.e. when the card is realized).</summary>
    public Avalonia.Media.Imaging.Bitmap? Cover
    {
        get
        {
            if (!_coverRequested)
            {
                _coverRequested = true;
                _ = LoadCoverAsync();
            }
            return _cover;
        }
    }

    public string Initials => string.Concat(Title.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Where(w => char.IsLetterOrDigit(w[0])).Take(2).Select(w => char.ToUpperInvariant(w[0])));

    private async Task LoadCoverAsync()
    {
        var source = new[] { _rom.Icon, _rom.Thumbnail, _rom.Banner, _rom.Images?.FirstOrDefault() }
            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
        if (source is null || CoverCache is null) return;
        var path = await CoverCache.GetAsync(source);
        if (path is null) return;
        try
        {
            _cover = await Task.Run(() =>
            {
                using var fs = FileUtils.OpenRead(path);
                return Avalonia.Media.Imaging.Bitmap.DecodeToWidth(fs, 160);
            });
            OnPropertyChanged(nameof(Cover));
        }
        catch (Exception ex) { FileUtils.Log($"[Image] {source}: {ex.Message}"); }
    }
    public string GetPropertyValue(string key)
    {
        return Properties.FirstOrDefault(p => p.Key == key).Value ?? string.Empty;
    }
    public ObservableCollection<KeyValuePair<string, string>> Properties { get; }

    // Helper property to allow dynamic binding by key — cached lazily.
    private Dictionary<string, string>? _propertiesDictionaryCache;
    public Dictionary<string, string> PropertiesDictionary
        => _propertiesDictionaryCache ??= BuildPropertiesDictionary();

    private Dictionary<string, string> BuildPropertiesDictionary()
    {
        var dict = Properties.ToDictionary(kv => kv.Key, kv => kv.Value);

        // Ensure all standard + extended properties always exist (prevents binding errors)
        foreach (var prop in new[] { "TitleID", "Title", "RomType", "Region", "Version",
                                      "FileName", "Publisher", "Developer", "Languages", "Genres",
                                      "Size", "Release Date", "Product Code", "Players",
                                      "Description", "Alt Titles", "Min Firmware", "Rating",
                                      "Hashes", "IsDemo", "File Path" })
        {
            dict.TryAdd(prop, string.Empty);
        }
        return dict;
    }
    public RomViewModel(Rom rom)
    {
        _rom = rom;
        Properties = [];
        LoadProperties();

        // Add derived class-specific properties
        AddDerivedClassProperties();
    }

    private void AddDerivedClassProperties()
    {
        // Get base Rom properties
        var baseProps = typeof(Rom).GetProperties().Select(p => p.Name).ToHashSet();
    
        // Also exclude any properties we already added in LoadProperties
        var alreadyAdded = Properties.Select(p => p.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Get properties from the actual runtime type that aren't in base Rom
        var derivedProps = _rom.GetType().GetProperties()
            .Where(p => !baseProps.Contains(p.Name) && p.GetMethod?.IsPublic == true)
            .ToList();
    
        // Add each unique property from the derived class (skip if key already exists)
        foreach (var prop in derivedProps)
        {
            if (alreadyAdded.Contains(prop.Name)) continue;
            var value = prop.GetValue(_rom)?.ToString() ?? "N/A";
            AddProperty(prop.Name, value);
        }
    }

    private void LoadProperties()
    {
        var standardProps = new[] { "TitleID", "Title", "Type", "Region", "Version" };
        AddProperty("TitleID", TitleID);
        AddProperty("Title", Title);
        AddProperty("Type", RomType);
        AddProperty("Region", Region);
        AddProperty("Version", Version);
        AddProperty("File Path", Path);
        // FileName (just the file/folder name, not full path)
        AddProperty("FileName", string.IsNullOrEmpty(Path) ? "" : System.IO.Path.GetFileName(Path));
        if (_rom.Size.HasValue)
            AddProperty("Size", FileUtils.FormatFileSize(_rom.Size.Value));
        if (_rom.ReleaseDate.HasValue)
            AddProperty("Release Date", _rom.ReleaseDate.Value.ToString("yyyy-MM-dd"));
        AddProperty("Publisher", _rom.Publisher ?? "");
        AddProperty("Developer", _rom.Developer ?? "");
        if (_rom.Languages?.Any() == true)
            AddProperty("Languages", string.Join(", ", _rom.Languages));
        if (_rom.Genres?.Any() == true)
            AddProperty("Genres", string.Join(", ", _rom.Genres));
        if (_rom.NumberOfPlayers > 0)
            AddProperty("Players", _rom.NumberOfPlayers.ToString());
        if (!string.IsNullOrEmpty(_rom.ProductCode))
            AddProperty("Product Code", _rom.ProductCode);
        if (!string.IsNullOrEmpty(_rom.MinimumFirmware))
            AddProperty("Min Firmware", _rom.MinimumFirmware);
        if (_rom.Descriptions?.Any() == true)
            AddProperty("Description", _rom.Descriptions.FirstOrDefault()?.Value ?? string.Empty);
        // Alt Titles (all titles except the primary one)
        if (_rom.Titles?.Count > 1)
        {
            var altTitles = _rom.Titles.Skip(1)
                .Where(t => !string.IsNullOrWhiteSpace(t.Value))
                .Select(t => $"{t.Value} [{t.Language}]");
            AddProperty("Alt Titles", string.Join("; ", altTitles));
        }
        if (_rom.Ratings?.Any() == true)
        {
            var rating = _rom.Ratings.FirstOrDefault();
            if (rating != null)
                AddProperty("Rating", $"{rating.Name} {rating.Age}+");
        }
        // Hashes
        if (_rom.Hashes?.Any() == true)
            AddProperty("Hashes", string.Join("; ", _rom.Hashes.Select(h => $"{h.Type}: {h.Value}")));
        // IsDemo flag
        if (_rom.IsDemo)
            AddProperty("IsDemo", "Yes");
        foreach (var key in standardProps)
        {
            if (!Properties.Any(p => p.Key == key))
                AddProperty(key, "");
        }
    }

    private void AddProperty(string key, string value)
    {
        Properties.Add(new KeyValuePair<string, string>(key, value ?? string.Empty));
    }

    private string DetermineRomType()
    {
        if (_rom is Game) return "Game";
        if (_rom is Update) return "Update";
        if (_rom is DLC) return "DLC";
        if (_rom is Homebrew) return "Homebrew";
        return "Unknown";
    }
}