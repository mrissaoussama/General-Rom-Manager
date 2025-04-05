// csharp - AvaloniaApplication1/AvaloniaUI/ViewModels/RomViewModel.cs

using RomManagerShared.Switch;

namespace AvaloniaUI.ViewModels;

public partial class RomViewModel : ViewModelBase
{
    private readonly Rom _rom;

    public string TitleID => _rom.TitleID;
    public string Title => _rom.Titles?.FirstOrDefault()?.Value ?? "Unknown";
    public string Region => _rom.Regions?.ToString() ?? "Unknown";
    public string Version => _rom.Version ?? "N/A";
    public string RomType => DetermineRomType();
    public string Path => _rom.Path;

    public ObservableCollection<KeyValuePair<string, string>> Properties { get; }

    // Helper property to allow dynamic binding by key
    public Dictionary<string, string> PropertiesDictionary =>
        Properties.ToDictionary(kv => kv.Key, kv => kv.Value);

    public RomViewModel(Rom rom)
    {
        _rom = rom;
        Properties = [];
        LoadProperties();
    }

    private void LoadProperties()
    {
        AddProperty("Title ID", TitleID);
        AddProperty("Title", Title);
        AddProperty("Type", RomType);
        AddProperty("Region", Region);
        AddProperty("Version", Version);
        AddProperty("File Path", Path);
        if (_rom.Size.HasValue)
            AddProperty("Size", FormatSize(_rom.Size.Value));
        if (_rom.ReleaseDate.HasValue)
            AddProperty("Release Date", _rom.ReleaseDate.Value.ToString("yyyy-MM-dd"));
        if (!string.IsNullOrEmpty(_rom.Publisher))
            AddProperty("Publisher", _rom.Publisher);
        if (!string.IsNullOrEmpty(_rom.Developer))
            AddProperty("Developer", _rom.Developer);
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
            AddProperty("Description", _rom.Descriptions.FirstOrDefault()?.Value);
        if (_rom.Ratings?.Any() == true)
        {
            var rating = _rom.Ratings.FirstOrDefault();
            AddProperty("Rating", $"{rating.Name} {rating.Age}+");
        }

        if (_rom is SwitchUpdate switchUpdate && !string.IsNullOrEmpty(switchUpdate.RelatedGameTitleName))
        {
            AddProperty("Related Game", switchUpdate.RelatedGameTitleName);
            AddProperty("Related Game ID", switchUpdate.RelatedGameTitleID);
        }
        else if (_rom is SwitchDLC switchDLC && !string.IsNullOrEmpty(switchDLC.RelatedGameTitleName))
        {
            AddProperty("Related Game", switchDLC.RelatedGameTitleName);
            AddProperty("Related Game ID", switchDLC.RelatedGameTitleID);
        }
    }

    private void AddProperty(string key, string value)
    {
        if (!string.IsNullOrEmpty(value)) Properties.Add(new KeyValuePair<string, string>(key, value));
    }

    private string FormatSize(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        int i;
        double dblSByte = bytes;
        for (i = 0; i < suffixes.Length && bytes >= 1024; i++, bytes /= 1024) dblSByte = bytes / 1024.0;
        return $"{dblSByte:0.##} {suffixes[i]}";
    }

    [RelayCommand]
    private void ViewDetails()
    {
    }

    [RelayCommand]
    private void RenameFile()
    {
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