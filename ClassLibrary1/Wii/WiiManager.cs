using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
using RomManagerShared.Wii.Parsers;
using RomManagerShared.Wii.TitleInfoProviders;
namespace RomManagerShared.Wii
{
    public class WiiManager : IConsoleManager
    {
        public RomParserExecutor RomParserExecutor { get; set; }
        private readonly WiiGameTDBInfoProvider titleInfoProvider;
        public HashSet<Rom> RomList { get; set; }        public WiiManager()
        {
            RomList = [];
            RomParserExecutor = new RomParserExecutor();
            RomParserExecutor
              .AddParser(new WiiWBFSRomParser())              .AddParser(new WiiWadRomParser()                );
            var titlesPath = RomManagerConfiguration.GetWiiGameTDBPath();
            if (titlesPath == null)
                FileUtils.Log("Wii titles path not found");
            else
            {
                titleInfoProvider = new WiiGameTDBInfoProvider();
                titleInfoProvider.Source = titlesPath;
            }
        }
        public async Task Setup()
        {
            List<Task> tasks =
            [
                titleInfoProvider.LoadTitleDatabaseAsync(),
            ];
            await Task.WhenAll(tasks);
        }
        public async Task ProcessFile(string file)
        {            var processedhash = await RomParserExecutor.ExecuteParsers(file);            var processedlist = processedhash.ToList();            for (int i = 0; i < processedlist.Count; i++)
            {
                if (processedlist[i].TitleID is null)
                    Console.WriteLine("index {0} is null, filepath={0}", i, file);
                else
                {
                    // Console.WriteLine(processedlist[i].TitleID + " " + processedlist[i].TitleName);
                    var rom = await titleInfoProvider.GetTitleInfo(processedlist[i]);
                    if (rom is not null)
                        processedlist[i] = rom;
                }
            }            RomList.UnionWith(processedlist);
        }
        public HashSet<string> GetSupportedExtensions()
        {
            if (RomParserExecutor.Parsers.Count == 0)
            {
                return [];
            }
            HashSet<string> extensionhashset = [];
            foreach (var parser in RomParserExecutor.Parsers)
            {
                extensionhashset.UnionWith(parser.Extensions);
            }
            return extensionhashset;
        }
    }}
