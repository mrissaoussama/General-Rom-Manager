using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.WiiU.Parsers;
using RomManagerShared.Utils;
using RomManagerShared.WiiU.Configuration;
namespace RomManagerShared.WiiU;

public class NintendoWiiUManager :ConsoleManager<WiiUConsole>
{
    public List<List<Rom>> GroupedRomList { get; set; }
    public NintendoWiiUManager(TitleInfoProviderManager<WiiUConsole> titleInfoProviderManager,
RomParserExecutor<WiiUConsole> romParserExecutor)
: base(romParserExecutor)

    {
        TitleInfoProviderManager = titleInfoProviderManager;
        RomList = [];
        GroupedRomList = [];
        var titlesPath = WiiUConfiguration.GetTitleDBPath();
        if (titlesPath == null)
            FileUtils.Log("WiiU titles path not found");
          }
    public override async Task Setup()
    {
        List<Task> tasks =
        [ WiiUWikiBrewScraper.ScrapeTitles()
        //titleInfoProvider.LoadTitleDatabaseAsync(),
        // UpdateVersionProvider.LoadVersionDatabaseAsync()
        ];
        await Task.WhenAll(tasks);
    }
    public void LoadGroupRomList()
    {
        //GroupedRomList = WiiUUtils.GroupRomList(RomList);
    }    public List<string> GetSupportedExtensions()
    {
        if (RomParserExecutor.Parsers.Count == 0)
        {
            return [];
        }
        List<string> extensionList = [];
        foreach (var parser in RomParserExecutor.Parsers)
        {
            extensionList.AddRange(parser.Extensions);
        }
        return extensionList;
    }
  
    public List<Rom> GetRomGroup(Rom rom)
    {
        var group = GroupedRomList.Where(group => group.Contains(rom))
            .SelectMany(group => group)
            .ToList();
        return group;
    }
}