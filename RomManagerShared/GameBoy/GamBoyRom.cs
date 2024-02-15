using RomManagerShared.Base;
namespace RomManagerShared.GameBoy;

public interface IGameBoyRom { }
public class GameBoyGame : Game, IGameBoyRom
{
    public GameBoyGame() : base()
    {
    }
}