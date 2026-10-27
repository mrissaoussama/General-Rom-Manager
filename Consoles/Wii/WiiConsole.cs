using RomManagerShared.Interfaces;

namespace RomManagerShared.Wii;

[ConsoleInfo("Nintendo Wii", "Nintendo", "Wii")]
public class WiiConsole : GamingConsole
{
    public WiiConsole() { Name = "Wii"; }
}
