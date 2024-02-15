using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.WiiU.Parsers;
using RomManagerShared.Utils;
using RomManagerShared.WiiU.Configuration;
namespace RomManagerShared.WiiU;

public class NintendoWiiUManager :ConsoleManager<NintendoWiiUConsole>
{
    public HashSet<HashSet<Rom>> GroupedRomList { get; set; }
    public NintendoWiiUManager()
    {
        RomList = [];
        GroupedRomList = [];
        RomParserExecutor = new RomParserExecutor();
        RomParserExecutor
         .AddParser(new WiiUTMDTIKParser())
         .AddParser(new WiiUWudWuxParser())
         .AddParser(new WiiUXmlParser())
             ;
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
    public override async Task ProcessFile(string file)
    {        var processedhash = await RomParserExecutor.ExecuteParsers(file);        var processedlist = processedhash.ToList();        for (int i = 0; i < processedlist.Count; i++)
        {
            var rom = processedlist[i];
            WiiUWikiBrewTitleDTO title = WiiUWikiBrewScraper. titles.FirstOrDefault(x =>
                (rom.TitleID != null && x.TitleID == rom.TitleID) ||
                (x.ProductCode != null && rom.ProductCode != null && rom.ProductCode.Contains(x.ProductCode)));
            if (title is not null)
                { 
                    if (title.Region == "JPN")
                        rom.AddRegion(Region.Japan);
                    if (title.Region == "EUR")
                        rom.AddRegion(Region.Europe);
                    if (title.Region == "USA")
                        rom.AddRegion(Region.USA);
                    rom.AddTitleName(title.Description);
                    if (!string.IsNullOrEmpty(rom.ProductCode))
                        rom.ProductCode = title.ProductCode;
                if (title.TitleID is not null && rom.TitleID is null)
                    rom.TitleID= title.TitleID;

                }
               
            }
              RomList.UnionWith(processedlist);
    }
      public void LoadGroupRomList()
    {
        //GroupedRomList = WiiUUtils.GroupRomList(RomList);
    }    public HashSet<string> GetSupportedExtensions()
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
  
    public HashSet<Rom> GetRomGroup(Rom rom)
    {
        var group = GroupedRomList.Where(group => group.Contains(rom))
            .SelectMany(group => group)
            .ToHashSet();
        return group;
    }
}