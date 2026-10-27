using RomManagerShared.Interfaces;

namespace RomManagerShared.PS3;

[ConsoleInfo("PlayStation 3", "Sony", "PS3")]
public class PS3Console : GamingConsole
{
    public PS3Console() { Name = "PS3"; }
}
