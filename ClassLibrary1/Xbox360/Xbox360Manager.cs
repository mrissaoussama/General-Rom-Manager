using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Xbox360.Parsers;
namespace RomManagerShared.Xbox360;

public class Xbox360Manager : IConsoleManager
{
    public RomParserExecutor RomParserExecutor { get; set; }
    public HashSet<Rom> RomList { get; set; }
    public Xbox360Manager()
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
        RomParserExecutor.AddParser(new Xbox360ISORomParser());
        RomParserExecutor.AddParser(new Xbox360XEXRomParser());
        return Task.CompletedTask;
    }
}
