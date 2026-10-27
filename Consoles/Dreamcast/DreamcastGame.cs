using RomManagerShared.Base;
namespace RomManagerShared.Dreamcast;

public interface IDreamcastRom { }
public class DreamcastGame : Game, IDreamcastRom
{
    [UiProperty(DisplayName = "Disc", Order = 20)]
    public int DiscNumber { get; set; } = 1;
}
