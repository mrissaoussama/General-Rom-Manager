using RomManagerShared.Base;
namespace RomManagerShared.PS3;

public interface IPS3Rom : IFolderLayout
{
    // <title>/sce_sys/param.sfo
    int IFolderLayout.FolderLevelsAboveMarker(string marker) => marker == "param.sfo" ? 2 : 0;
}
public class PS3Game : Game, IPS3Rom
{
    public PS3Game() : base()
    {
    }
}
public class PS3DLC : DLC, IPS3Rom
{
    public PS3DLC() : base()
    {
    }
}
public class PS3Update : Update, IPS3Rom
{
    public PS3Update() : base()
    {
    }
}
