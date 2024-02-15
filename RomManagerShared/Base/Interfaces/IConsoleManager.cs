using RomManagerShared.Base;namespace RomManagerShared.Interfaces;

public interface IConsoleManager
{
    RomParserExecutor RomParserExecutor { get; set; }
    HashSet<Rom> RomList { get; set; }
    Task ProcessFile(string file);
    Task Setup();
}
public abstract class ConsoleManager<T> :IConsoleManager where T : GamingConsole
{
    public HashSet<Rom> RomList { get; set; }
    public RomParserExecutor RomParserExecutor { get; set; }

    public abstract Task ProcessFile(string file);
    public abstract Task Setup();
}

    public class GamingConsole
    {
    public string Name { get; set; } = "";
    }

public class NintendoSwitchConsole : GamingConsole
{
    public NintendoSwitchConsole()
    {
        Name = "Switch";
    }
}

public class ThreeDSConsole : GamingConsole
{
    public ThreeDSConsole()
    {
        Name = "3DS";
    }
}

public class DSConsole : GamingConsole
{
    public DSConsole()
    {
        Name = "DS";
    }
}

public class GameBoyConsole : GamingConsole
{
    public GameBoyConsole()
    {
        Name = "GameBoy";
    }
}

public class GameBoyAdvanceConsole : GamingConsole
{
    public GameBoyAdvanceConsole()
    {
        Name = "GameBoy Advance";
    }
}

public class PS4Console : GamingConsole
{
    public PS4Console()
    {
        Name = "PS4";
    }
}

public class PSPConsole : GamingConsole
{
    public PSPConsole()
    {
        Name = "PSP";
    }
}

public class SNESConsole : GamingConsole
{
    public SNESConsole()
    {
        Name = "SNES";
    }
}

public class WiiConsole : GamingConsole
{
    public WiiConsole()
    {
        Name = "Wii";
    }
}

public class Nintendo64Console : GamingConsole
{
    public Nintendo64Console()
    {
        Name = "Nintendo 64";
    }
}

public class SegaSaturnConsole : GamingConsole
{
    public SegaSaturnConsole()
    {
        Name = "Sega Saturn";
    }
}

public class PSVitaConsole : GamingConsole
{
    public PSVitaConsole()
    {
        Name = "PS Vita";
    }
}

public class OriginalXboxConsole : GamingConsole
{
    public OriginalXboxConsole()
    {
        Name = "Original Xbox";
    }
}

public class Xbox360Console : GamingConsole
{
    public Xbox360Console()
    {
        Name = "Xbox 360";
    }
}

public class PS3Console : GamingConsole
{
    public PS3Console()
    {
        Name = "PS3";
    }
}

public class PS2Console : GamingConsole
{
    public PS2Console()
    {
        Name = "PS2";
    }
}

public class NintendoWiiUConsole : GamingConsole
{
    public NintendoWiiUConsole()
    {
        Name = "Nintendo Wii U";
    }
}

