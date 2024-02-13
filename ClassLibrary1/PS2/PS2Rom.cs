using RomManagerShared.Base;
namespace RomManagerShared.PS2;

public interface IPS2Rom { }
public class PS2Game : Game, IPS2Rom
{
    public PS2Game() : base()
    {
    }
}