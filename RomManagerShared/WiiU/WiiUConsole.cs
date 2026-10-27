using RomManagerShared.Interfaces;

namespace RomManagerShared.WiiU;

[ConsoleInfo("Nintendo Wii U", "Nintendo", "WiiU")]
public class WiiUConsole : GamingConsole
{
    public WiiUConsole() { Name = "Wii U"; }
}
