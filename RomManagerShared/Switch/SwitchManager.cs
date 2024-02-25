using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Switch.Configuration;
using RomManagerShared.Switch.Parsers;
using RomManagerShared.Switch.TitleInfoProviders;
using RomManagerShared.Utils;
namespace RomManagerShared.Switch;

public class SwitchManager : ConsoleManager<SwitchConsole>, IRomMissingContentChecker
{
    public SwitchUpdateVersionProvider? UpdateVersionProvider { get; set; }
    public List<List<Rom>> GroupedRomList { get; set; }
    public SwitchManager(TitleInfoProviderManager<SwitchConsole> titleInfoProviderManager,
RomParserExecutor<SwitchConsole> romParserExecutor)
: base(romParserExecutor,titleInfoProviderManager)
    {
        GroupedRomList = [];

        var versionjsonPath = SwitchConfiguration.GetVersionsPath();
        if (string.IsNullOrEmpty(versionjsonPath))
        {
            FileUtils.Log("Switch version path not found");
            return;
        }
        UpdateVersionProvider = new(versionjsonPath);    }

 
    public async Task<List<RomMissingUpdates>> GetMissingUpdates()
    {
        if (UpdateVersionProvider is null)
        { throw new Exception("Update Version Provider is null"); }
        LoadGroupRomList();
        List<RomMissingUpdates> set = [];
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
    public async Task<RomMissingUpdates?> GetMissingUpdates(Rom rom)
    {
        var latestUpdateVersion = await UpdateVersionProvider.GetLatestVersion(rom.TitleID);
        if (latestUpdateVersion is null or { Version: "0" })
        {
            return null;
        }
        RomMissingUpdates? missing = null;
        List<SwitchUpdate> localupdates = GetRomGroup(rom).OfType<SwitchUpdate>().ToList();
        if (localupdates.Count == 0)
        {
            missing = new();
            var latestVersion = new SwitchUpdate
            {
                TitleID = rom.TitleID,
                Version = latestUpdateVersion.Version
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
        if (latestLocalUpdate < int.Parse(latestUpdateVersion.Version))
        {
            missing = new();
            var latestVersion = new SwitchUpdate
            {
                TitleID = rom.TitleID,
                Version = latestUpdateVersion.Version
            };
            missing.LatestUpdate = latestVersion;
            missing.LocalUpdates = [];
            missing.LocalUpdates.AddRange(localupdates);
            missing.Rom = rom;
        }
        return missing;
    }

    Task<List<RomMissingDLCs>> GetMissingDLC()
    {
        throw new NotImplementedException();
    }

    public Task<RomMissingDLCs> GetMissingDLC(Rom rom)
    {
        return null;
    }

    Task<List<RomMissingDLCs>> IRomMissingContentChecker.GetMissingDLC()
    {
        return null;
    }

    public List<Rom> GetRomGroup(Rom rom)
    {
        var group = GroupedRomList.Where(group => group.Contains(rom))
            .SelectMany(group => group)
            .ToList();
        return group;
    }
}