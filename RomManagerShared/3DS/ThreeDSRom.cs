using RomManagerShared.Base;namespace RomManagerShared.ThreeDS;

public interface IThreeDSRom { }
public class ThreeDSGame : Game, IThreeDSRom
{
    public ThreeDSGame() : base()
    {
    }
}
public class ThreeDSDLC : DLC, IThreeDSRom
{
    public ThreeDSDLC() : base()
    {
    }
}
public class ThreeDSUpdate : Update, IThreeDSRom
{
    public ThreeDSUpdate() : base()
    {
    }
}
