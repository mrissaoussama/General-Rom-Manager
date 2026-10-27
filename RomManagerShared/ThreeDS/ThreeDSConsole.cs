using RomManagerShared.Interfaces;

namespace RomManagerShared.ThreeDS;

[ConsoleInfo("Nintendo 3DS", "Nintendo", "3DS")]
public class ThreeDSConsole : GamingConsole
{
    public ThreeDSConsole() { Name = "3DS"; }
}
