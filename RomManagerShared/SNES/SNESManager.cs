using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.SNES.Parsers;
namespace RomManagerShared.SNES;

public class SNESManager : IConsoleManager
{
    public RomParserExecutor RomParserExecutor { get; set; }
    public HashSet<Rom> RomList { get; set; }
    public SNESManager()
    {
        RomList = [];
        RomParserExecutor = new RomParserExecutor();
    }
    public async Task ProcessFile(string file)
    {
        var processedlist = await RomParserExecutor.ExecuteParsers(file);
        RomList.UnionWith(processedlist);
    }
    public Task Setup()
    {
        RomParserExecutor.AddParser(new SNESRomParser());
        return Task.CompletedTask;
    }
    public static bool IsValidRom(string path)
    {
        return SNESUtils.IsSNESRom(path);
    }
}
