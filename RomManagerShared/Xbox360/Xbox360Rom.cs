using RomManagerShared.Base;
namespace RomManagerShared.Xbox360;

public interface IXbox360Rom : IFolderLayout
{
    int IFolderLayout.FolderLevelsAboveMarker(string marker) => marker == "default.xex" ? 1 : 0;
}
public class Xbox360Game : Game, IXbox360Rom
{
    public Xbox360Game() : base()
    {
    }
}
public class Xbox360Update : Update, IXbox360Rom
{
    public Xbox360Update() : base()
    {
    }
}
public class Xbox360DLC : DLC, IXbox360Rom
{
    public Xbox360DLC() : base()
    {
    }
}
