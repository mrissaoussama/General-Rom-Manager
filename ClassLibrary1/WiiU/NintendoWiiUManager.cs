﻿using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.WiiU.Parsers;
using RomManagerShared.Utils;
namespace RomManagerShared.WiiU;

public class NintendoWiiUManager : IConsoleManager
{
    public RomParserExecutor RomParserExecutor { get; set; }
    public HashSet<Rom> RomList { get; set; }
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
        var titlesPath = RomManagerConfiguration.GetWiiUTitleDBPath();
        if (titlesPath == null)
            FileUtils.Log("WiiU titles path not found");
      
    public async Task Setup()
    {
        List<Task> tasks =
        [ WiiUWikiBrewScraper.ScrapeTitles()
        //titleInfoProvider.LoadTitleDatabaseAsync(),
        // UpdateVersionProvider.LoadVersionDatabaseAsync()
        ];
        await Task.WhenAll(tasks);
    }
    public async Task ProcessFile(string file)
    {
        {
            var rom = processedlist[i];
            TitleDTO title = WiiUWikiBrewScraper. titles.FirstOrDefault(x =>
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
      
    }
  
    {
        //GroupedRomList = WiiUUtils.GroupRomList(RomList);
    }
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