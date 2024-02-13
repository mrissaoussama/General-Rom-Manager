using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.SegaSaturn.Parsers;
namespace RomManagerShared.SegaSaturn;

public class SegaSaturnManager : IConsoleManager
{
    public RomParserExecutor RomParserExecutor { get; set; }
    public HashSet<Rom> RomList { get; set; }
    public SegaSaturnManager()
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
        RomParserExecutor.AddParser(new SegaSaturnRomParser());
        return Task.CompletedTask;
    }
}
