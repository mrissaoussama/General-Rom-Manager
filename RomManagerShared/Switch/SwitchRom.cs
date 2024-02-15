using RomManagerShared.Base;namespace RomManagerShared.Switch;

public interface ISwitchRom { }
public class SwitchGame : Game, ISwitchRom
{
    public SwitchGame() : base()
    {
    }
}
public class SwitchDLC : DLC, ISwitchRom
{
    public string? RelatedGameTitleID { get; set; }
    public string? RelatedGameTitleName { get; set; }

    public SwitchDLC() : base()
    {
    }}
public class SwitchUpdate : Update, ISwitchRom
{
    public string? RelatedGameTitleID { get; set; }
    public string? RelatedGameTitleName { get; set; }

    public SwitchUpdate() : base()
    {
    }
}
