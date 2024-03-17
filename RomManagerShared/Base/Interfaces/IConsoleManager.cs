using RomManagerShared.Base;

namespace RomManagerShared.Interfaces;

public interface IConsoleManager<T>  where T : GamingConsole
{
    RomParserExecutor<T> RomParserExecutor { get; set; }
    List<Rom> RomList { get; set; }
    Task ProcessFile(string file);
    Task Setup();
}
public  class ConsoleManager<T> :IConsoleManager<T> where T : GamingConsole
{
    public TitleInfoProviderManager<T>? TitleInfoProviderManager { get; set; }
    public RomParserExecutor<T> RomParserExecutor { get; set; }
    public List<Rom> RomList { get; set; }

    public ConsoleManager(RomParserExecutor<T> romParserExecutor, TitleInfoProviderManager<T> titleInfoProviderManager)
    {
        TitleInfoProviderManager = titleInfoProviderManager;
        RomList = [];
        RomParserExecutor = romParserExecutor;
    }
    public ConsoleManager(RomParserExecutor<T> romParserExecutor)
    {
        RomList = [];
        RomParserExecutor = romParserExecutor;
    }

    public virtual async Task ProcessFile(string file)
    {
        var processedhash = await RomParserExecutor.ExecuteParsers(file);
        if (RomParserExecutor.Parsers.Count == 0)
        {
           // processedhash= await IdentifyRomFromHash(file);
        }
        var processedlist = processedhash.ToList();
        RomList.AddRange(processedlist);
    }

    public virtual Task IdentifyRomFromHash(string file)
    {
        return Task.CompletedTask;
    }


    public virtual async Task Setup()
    {
        if(TitleInfoProviderManager is not null)
        await TitleInfoProviderManager.Setup();
    }

}
#region console classes
public class GamingConsole
    {
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
}

public class SwitchConsole : GamingConsole
{
    public SwitchConsole()
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

public class N64Console : GamingConsole
{
    public N64Console()
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

public class WiiUConsole : GamingConsole
{
    public WiiUConsole()
    {
        Name = "Wii U";
    }
}

#endregion