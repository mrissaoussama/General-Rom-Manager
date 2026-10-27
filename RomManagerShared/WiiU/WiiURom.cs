using RomManagerShared.Base;
namespace RomManagerShared.WiiU;

public interface IWiiURom : IFolderLayout
{
    // <title>/code/app.xml, <title>/title.tmd
    int IFolderLayout.FolderLevelsAboveMarker(string marker) => marker switch { "app.xml" => 2, "title.tmd" => 1, _ => 0 };
}
public class WiiUGame : Game, IWiiURom
{
    public WiiUGame() : base()
    {
    }
}
public class WiiUDLC : DLC, IWiiURom
{
    public string? RelatedGameTitleID { get; set; }
    public string? RelatedGameTitleName { get; set; }

    public WiiUDLC() : base()
    {
    }
}
public class WiiUUpdate : Update, IWiiURom
{
    public string? RelatedGameTitleID { get; set; }
    public string? RelatedGameTitleName { get; set; }

    public WiiUUpdate() : base()
    {
    }
}
