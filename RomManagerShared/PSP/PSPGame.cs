using RomManagerShared.Base;
namespace RomManagerShared.PSP;

public interface IPSPRom { }
public class PSPGame : Game, IPSPRom
{
    public PSPGame() : base()
    {
    }    public int DiskNumber { get; internal set; }
    public int DiskTotal { get; internal set; }
    public int ParentalLevel { get; internal set; }
    public string Category { get; internal set; }
}
public class PSPUpdate : Update, IPSPRom
{
    public PSPUpdate() : base()
    {
    }    public int DiskNumber { get; internal set; }
    public int DiskTotal { get; internal set; }
    public int ParentalLevel { get; internal set; }
    public string? Category { get; internal set; }
}
public class PSPDLC : DLC, IPSPRom
{
    public PSPDLC() : base()
    {
    }    public int DiskNumber { get; internal set; }
    public int DiskTotal { get; internal set; }
    public int ParentalLevel { get; internal set; }
    public string Category { get; internal set; }
}