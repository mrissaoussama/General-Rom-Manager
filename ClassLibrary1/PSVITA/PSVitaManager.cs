using RomManagerShared.Base;
using RomManagerShared.PSVita.Parsers;
using RomManagerShared.Interfaces;
namespace RomManagerShared.PSVita
{
    public class PSVitaManager : IConsoleManager
    {
        public RomParserExecutor RomParserExecutor { get; set; }
        public HashSet<Rom> RomList { get; set; }
        public PSVitaManager()
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
            RomParserExecutor.AddParser(new PSVitaVPKRomParser());
            RomParserExecutor.AddParser(new PSVitaFolderRomParser());
            return Task.CompletedTask;
        }
    }
}
