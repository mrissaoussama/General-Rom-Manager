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
public class ThreeDSDSIWare : Game, IThreeDSRom
{
    public ThreeDSDSIWare() : base()
    {
    }
}
public class ThreeDSVirtualConsole : Game, IThreeDSRom
{
    public ThreeDSVirtualConsole() : base()
    {
    }
}
