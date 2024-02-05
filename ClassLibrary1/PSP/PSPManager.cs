using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.PSP.Parsers;
namespace RomManagerShared.PSP
{
    public class PSPManager : IConsoleManager
    {
        public RomParserExecutor RomParserExecutor { get; set; }
        public HashSet<Rom> RomList { get; set; }
        public PSPManager()
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
            RomParserExecutor.AddParser(new PSPRomParser());
            return Task.CompletedTask;
        }
    }
}
