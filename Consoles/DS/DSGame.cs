using RomManagerShared.Base;
namespace RomManagerShared.DS;

public interface IDSRom { }
public class DSGame : Game, IDSRom
{
    public DSGame() : base()
    {
    }
}