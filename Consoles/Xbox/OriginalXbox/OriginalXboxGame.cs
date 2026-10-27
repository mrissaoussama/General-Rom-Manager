using RomManagerShared.Base;
namespace RomManagerShared.OriginalXbox;

public interface IOriginalXboxRom : IFolderLayout
{
    int IFolderLayout.FolderLevelsAboveMarker(string marker) => marker == "default.xbe" ? 1 : 0;
}
public class OriginalXboxGame : Game, IOriginalXboxRom
{
    public OriginalXboxGame() : base()
    {
    }
}
