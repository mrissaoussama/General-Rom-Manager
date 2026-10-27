using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.ThreeDS.Configuration;
using RomManagerShared.ThreeDS.TitleInfoProviders;
using RomManagerShared.Utils;

namespace RomManagerShared.ThreeDS;
public class ThreeDSManager : ConsoleManager<ThreeDSConsole>, IRomMissingContentChecker
{
    public ThreeDSManager(TitleInfoProviderManager<ThreeDSConsole> titleInfoProviderManager,
        RomParserExecutor<ThreeDSConsole> romParserExecutor)
        : base(romParserExecutor)
    {
        TitleInfoProviderManager = titleInfoProviderManager;
        GroupedRomList = [];
    }

    public List<List<Rom>> GroupedRomList { get ; set ; }

    public Task<List<RomMissingDLCs>> GetMissingDLC()
    {
        AppLog.Warn("[3DS] GetMissingDLC not yet implemented");
        return Task.FromResult(new List<RomMissingDLCs>());
    }

    public Task<RomMissingDLCs> GetMissingDLC(Rom rom)
    {
        AppLog.Warn("[3DS] GetMissingDLC(rom) not yet implemented");
        return Task.FromResult(new RomMissingDLCs());
    }

    public Task<List<RomMissingUpdates>> GetMissingUpdates()
    {
        AppLog.Warn("[3DS] GetMissingUpdates not yet implemented");
        return Task.FromResult(new List<RomMissingUpdates>());
    }

    public Task<RomMissingUpdates?> GetMissingUpdates(Rom rom)
    {
        AppLog.Warn("[3DS] GetMissingUpdates(rom) not yet implemented");
        return Task.FromResult<RomMissingUpdates?>(null);
    }

    public List<Rom> GetRomGroup(Rom rom)
    {
        var group = GroupedRomList.Where(group => group.Contains(rom))
            .SelectMany(group => group)
            .ToList();
        return group;
    }

    public void LoadGroupRomList()
    {
        GroupedRomList = ThreeDSUtils.GroupRomList(RomList);
    }
}
