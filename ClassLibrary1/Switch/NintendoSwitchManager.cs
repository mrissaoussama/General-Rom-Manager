using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Switch.Parsers;
using RomManagerShared.Switch.TitleInfoProviders;
using RomManagerShared.Utils;
namespace RomManagerShared.Switch;

public class NintendoSwitchManager : IConsoleManager, IRomMissingContentChecker
{
    public RomParserExecutor RomParserExecutor { get; set; }
    private readonly SwitchJsonTitleInfoProvider titleInfoProvider;
    public SwitchUpdateVersionProvider? UpdateVersionProvider { get; set; }
    public HashSet<Rom> RomList { get; set; }    public HashSet<HashSet<Rom>> GroupedRomList { get; set; }
    public NintendoSwitchManager()
    {
        RomList = [];
        GroupedRomList = [];
        RomParserExecutor = new RomParserExecutor();
        RomParserExecutor
         .AddParser(new SwitchRomNSPXCIParser())
          .AddParser(new SwitchRomNSPNSZParser())             ;
        var titlesPath = RomManagerConfiguration.GetSwitchTitleDBPath();
        if (titlesPath == null)
            FileUtils.Log("Switch titles path not found");
        titleInfoProvider = new SwitchJsonTitleInfoProvider(titlesPath);
        var versionjsonPath = RomManagerConfiguration.GetSwitchVersionsPath();
        if (string.IsNullOrEmpty(versionjsonPath))
        {
            FileUtils.Log("Switch version path not found");
            return;
        }
        UpdateVersionProvider = new(versionjsonPath);    }
    public async Task Setup()
    {
        List<Task> tasks =
        [
        //titleInfoProvider.LoadTitleDatabaseAsync(),
        // UpdateVersionProvider.LoadVersionDatabaseAsync()
        ];
        await Task.WhenAll(tasks);
    }
    public async Task ProcessFile(string file)
    {        var processedhash = await RomParserExecutor.ExecuteParsers(file);        var processedlist = processedhash.ToList();        for (int i = 0; i < processedlist.Count; i++)
        {
            if (processedlist[i].TitleID is null)
                Console.WriteLine("index {0} is null, filepath={0}", i, file);
            else
            {
                // Console.WriteLine(processedlist[i].TitleID + " " + processedlist[i].TitleName);
                //var rom = await titleInfoProvider.GetTitleInfo(processedlist[i]);
                //    if (rom is not null)
                //     processedlist[i] = rom;
            }
        }        RomList.UnionWith(processedlist);
    }
    public async Task<HashSet<RomMissingUpdates>> GetMissingUpdates()
    {
        if (UpdateVersionProvider is null)
        { throw new Exception("Update Version Provider is null"); }
        LoadGroupRomList();
        HashSet<RomMissingUpdates> set = [];
        foreach (var romGroup in GroupedRomList)
        {
            var missingreport = await GetMissingUpdates(romGroup.First());
            if (missingreport is not null)
                set.Add(missingreport);
        }
        return set;
    }    public void LoadGroupRomList()
    {
        GroupedRomList = SwitchUtils.GroupRomList(RomList);
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
    public async Task<RomMissingUpdates?> GetMissingUpdates(Rom rom)
    {
        var latestUpdateVersion = await UpdateVersionProvider.GetLatestVersion(rom.TitleID);
        if (latestUpdateVersion is null or "0")
        {
            return null;
        }
        RomMissingUpdates? missing = null;
        HashSet<SwitchUpdate> localupdates = GetRomGroup(rom).OfType<SwitchUpdate>().ToHashSet();
        if (localupdates.Count == 0)
        {
            missing = new();
            var latestVersion = new SwitchUpdate
            {
                TitleID = rom.TitleID,
                Version = latestUpdateVersion
            };
            missing.LatestUpdate = latestVersion;
            missing.LocalUpdates = null;
            missing.Rom = rom;
        }
        if (localupdates.Count == 0)
            return missing;
        var latestLocalUpdate = localupdates
            .Select(x => int.TryParse(x.Version, out var versionAsInt) ? versionAsInt : 0)
            .Max();
        if (latestLocalUpdate < int.Parse(latestUpdateVersion))
        {
            missing = new();
            var latestVersion = new SwitchUpdate
            {
                TitleID = rom.TitleID,
                Version = latestUpdateVersion
            };
            missing.LatestUpdate = latestVersion;
            missing.LocalUpdates = [];
            missing.LocalUpdates.UnionWith(localupdates);
            missing.Rom = rom;
        }
        return missing;
    }

    Task<HashSet<RomMissingDLCs>> GetMissingDLC()
    {
        throw new NotImplementedException();
    }

    public Task<RomMissingDLCs> GetMissingDLC(Rom rom)
    {
        return null;
    }

    Task<HashSet<RomMissingDLCs>> IRomMissingContentChecker.GetMissingDLC()
    {
        return null;
    }

    public HashSet<Rom> GetRomGroup(Rom rom)
    {
        var group = GroupedRomList.Where(group => group.Contains(rom))
            .SelectMany(group => group)
            .ToHashSet();
        return group;
    }
}