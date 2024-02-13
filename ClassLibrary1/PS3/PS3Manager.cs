using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.PS3.Parsers;
namespace RomManagerShared.PS3;

public class PS3Manager : IConsoleManager
{
    public RomParserExecutor RomParserExecutor { get; set; }
    public HashSet<Rom> RomList { get; set; }
    public PS3Manager()
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
        RomParserExecutor.AddParser(new PS3PKGRomParser());
        RomParserExecutor.AddParser(new PS3FolderRomParser());
        return Task.CompletedTask;
    }
}
