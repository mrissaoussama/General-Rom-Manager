using RomManagerShared.Base;
namespace RomManagerShared.Nintendo64;

public interface INintendo64Rom { }
public class Nintendo64Game : Game, INintendo64Rom
{
    public Nintendo64Game() : base()
    {
    }
}