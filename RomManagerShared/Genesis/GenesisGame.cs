using RomManagerShared.Base;

namespace RomManagerShared.Genesis;

public class GenesisGame : Game
{
    [UiProperty(DisplayName = "System")]
    public string? SystemType { get; set; }

    [UiProperty(DisplayName = "Domestic Name")]
    public string? DomesticName { get; set; }

    [UiProperty(DisplayName = "Copyright")]
    public string? Copyright { get; set; }

    [UiProperty(DisplayName = "Header")]
    public string? HeaderFormat { get; set; }
}
