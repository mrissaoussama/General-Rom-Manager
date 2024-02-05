using RomManagerShared.Base;
using RomManagerShared.GameBoy.Parsers;
using RomManagerShared.Interfaces;
namespace RomManagerShared.GameBoy
{
    public class GameBoyManager : IConsoleManager
    {
        public RomParserExecutor RomParserExecutor { get; set; }
        public HashSet<Rom> RomList { get; set; }
        public GameBoyManager()
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
            RomParserExecutor.AddParser(new GameBoyRomParser());
            return Task.CompletedTask;
        }
    }
}
