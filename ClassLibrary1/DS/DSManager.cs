using RomManagerShared.Base;
using RomManagerShared.DS.Parsers;
using RomManagerShared.Interfaces;
namespace RomManagerShared.DS;

public class DSManager : IConsoleManager
{
    public RomParserExecutor RomParserExecutor { get; set; }
    public HashSet<Rom> RomList { get; set; }
    public DSManager()
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
        RomParserExecutor.AddParser(new DSRomParser());
        return Task.CompletedTask;
    }
}
