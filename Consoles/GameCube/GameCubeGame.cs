using RomManagerShared.Base;
namespace RomManagerShared.GameCube;

public interface IGameCubeRom { }
public class GameCubeGame : Game, IGameCubeRom
{
    [UiProperty(DisplayName = "Disc", Order = 20)]
    public int DiscNumber { get; set; } = 1;
}
