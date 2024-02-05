using RomManagerShared.Base;
using RomManagerShared.OriginalXbox.Parsers;
using RomManagerShared.Interfaces;
namespace RomManagerShared.OriginalXbox
{
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
            RomParserExecutor.AddParser(new OriginalXboxRomParser());
            return Task.CompletedTask;
        }
    }
}
