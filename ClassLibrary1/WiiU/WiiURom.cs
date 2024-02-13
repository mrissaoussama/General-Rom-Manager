using RomManagerShared.Base;namespace RomManagerShared.WiiU;

public interface IWiiURom { }
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
    }}
public class WiiUUpdate : Update, IWiiURom
{
    public string? RelatedGameTitleID { get; set; }
    public string? RelatedGameTitleName { get; set; }

    public WiiUUpdate() : base()
    {
    }
}
