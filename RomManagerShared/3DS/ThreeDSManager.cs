using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Switch;
using RomManagerShared.ThreeDS.Configuration;
using RomManagerShared.ThreeDS.TitleInfoProviders;

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
        throw new NotImplementedException();
    }

    public Task<RomMissingDLCs> GetMissingDLC(Rom rom)
    {
        throw new NotImplementedException();
    }

    public Task<List<RomMissingUpdates>> GetMissingUpdates()
    {
        throw new NotImplementedException();
    }

    public Task<RomMissingUpdates?> GetMissingUpdates(Rom rom)
    {
        throw new NotImplementedException();
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
