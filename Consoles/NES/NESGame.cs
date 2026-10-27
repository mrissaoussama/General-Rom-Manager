using RomManagerShared.Base;

namespace RomManagerShared.NES;

public class NESGame : Game
{
    [UiProperty(DisplayName = "Mapper")]
    public int Mapper { get; set; }

    [UiProperty(DisplayName = "PRG ROM")]
    public string? PrgRom { get; set; }

    [UiProperty(DisplayName = "CHR ROM")]
    public string? ChrRom { get; set; }

    [UiProperty(DisplayName = "Header")]
    public string? HeaderFormat { get; set; }
}
