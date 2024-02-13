using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.OriginalXbox.Parsers;
namespace RomManagerShared.OriginalXbox;

public class OriginalXboxManager : IConsoleManager
{
    public RomParserExecutor RomParserExecutor { get; set; }
    public HashSet<Rom> RomList { get; set; }
    public OriginalXboxManager()
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
        RomParserExecutor.AddParser(new OriginalXboxXBERomParser());

        RomParserExecutor.AddParser(new OriginalXboxRomParser());
        RomParserExecutor.AddParser(new OriginalXboxISORomParser());
        return Task.CompletedTask;
    }
}
