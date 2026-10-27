using RomManagerShared.Interfaces;

namespace RomManagerShared.PS4;

[ConsoleInfo("PlayStation 4", "Sony", "PS4")]
public class PS4Console : GamingConsole
{
    public PS4Console() { Name = "PS4"; }
}
