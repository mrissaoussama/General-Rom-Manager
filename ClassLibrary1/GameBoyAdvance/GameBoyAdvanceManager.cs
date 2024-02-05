using RomManagerShared.Base;
using RomManagerShared.GameBoyAdvance.Parsers;
using RomManagerShared.Interfaces;
namespace RomManagerShared.GameBoyAdvance
{
    public class GameBoyAdvanceManager : IConsoleManager
    {
        public RomParserExecutor RomParserExecutor { get; set; }
        public HashSet<Rom> RomList { get; set; }
        public GameBoyAdvanceManager()
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
            RomParserExecutor.AddParser(new GameBoyAdvanceRomParser());
            return Task.CompletedTask;
        }
    }
}
