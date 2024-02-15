using RomManagerShared.Base;namespace RomManagerShared.Wii;

public interface IWiiRom { }
public class WiiGame : Game, IWiiRom
{
    public WiiGame() : base()
    {
    }
}
public class WiiWadGame : Game, IWiiRom
{
    public WiiWadGame() : base()
    {
    }    public string ChannelType { get; internal set; }
}
