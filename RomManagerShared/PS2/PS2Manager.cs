using RomManagerShared.Base;
using RomManagerShared.PS2.Parsers;
using RomManagerShared.Interfaces;
namespace RomManagerShared.PS2;

public class PS2Manager : IConsoleManager
{
    public RomParserExecutor RomParserExecutor { get; set; }
    public HashSet<Rom> RomList { get; set; }
    public PS2Manager()
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
        RomParserExecutor.AddParser(new PS2RomParser());
        return Task.CompletedTask;
    }
}
