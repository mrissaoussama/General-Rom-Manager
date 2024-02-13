using RomManagerShared.Base;
namespace RomManagerShared.SegaSaturn;

public interface ISegaSaturnRom { }
public class SegaSaturnGame : Game, ISegaSaturnRom
{
    public SegaSaturnGame() : base()
    {
    }
}