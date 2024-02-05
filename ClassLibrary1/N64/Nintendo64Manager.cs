using RomManagerShared.Base;
using RomManagerShared.N64.Parsers;
using RomManagerShared.Interfaces;
namespace RomManagerShared.N64
{
    public class Nintendo64Manager : IConsoleManager
    {
        public RomParserExecutor RomParserExecutor { get; set; }
        public HashSet<Rom> RomList { get; set; }
        public Nintendo64Manager()
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
            RomParserExecutor.AddParser(new Nintendo64RomParser());
            return Task.CompletedTask;
        }
    }
}
