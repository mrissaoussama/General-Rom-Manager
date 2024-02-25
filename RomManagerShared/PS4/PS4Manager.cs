using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.PS4.Parsers;
namespace RomManagerShared.PS4;

public class PS4Manager : ConsoleManager<PS4Console>, IRomMissingContentChecker
{
 
    public List<List<Rom>> GroupedRomList { get; set; }
    PS4PKGUpdateAndDLCChecker Checker { get; set; }
    public PS4Manager(
RomParserExecutor<PS4Console> romParserExecutor)
: base(romParserExecutor)
    {
        GroupedRomList=[]; Checker = new();
    }
    public void LoadGroupRomList()
    {
        GroupedRomList = PS4Utils.GroupRomList(RomList);
    }
    public async Task<List<Rom>> GetMissingDLC(Rom ps4game)
    {
        List<Rom> dlclist = [];
        List<Rom> missingdlc = [];
        // list.Add( await checker.CheckForUpdate(rom));
        if (ps4game is not PS4Game)
            return missingdlc;
        dlclist.AddRange(await Checker.GetDLCList(ps4game));
        if (dlclist.Count == 0)
        {
            return dlclist;
        }
        missingdlc = await Checker.GetMissingDLC(ps4game, RomList, dlclist);
        return missingdlc;
    }
    public async Task<Rom> GetMissingUpdate(Rom ps4game)
    {
        if (ps4game is not PS4Game)
            return null;
        Rom? latestUpdate = GetLatestLocalUpdate(ps4game);
        var onlineUpdate = await Checker.CheckForUpdate(ps4game);
        if (onlineUpdate != null)
        {
            if (latestUpdate is not null && Version.Parse(onlineUpdate.Version) > Version.Parse(((PS4Update)latestUpdate).Version))
            {
                return onlineUpdate;
            }
        }
        else
        {
            return null;
        }
        return onlineUpdate;
    }
    public Rom? GetLatestLocalUpdate(Rom ps4game)
    {
        List<Rom> relatedUpdates = RomList
            .Where(rom => rom.TitleID == ps4game.TitleID && rom is PS4Update)
            .ToList();
        if (relatedUpdates.Count == 0)
        {
            return null;
        }
        // Find the update with the highest version
        PS4Update latestUpdate = (PS4Update)relatedUpdates
            .OrderByDescending(rom => Version.Parse(((PS4Update)rom).Version))
            .First();
        Console.WriteLine("Updates found:");
        foreach (var update in relatedUpdates)
        {
            Console.WriteLine($"Version: {((PS4Update)update).Version}");
        }
        return latestUpdate;
    }

    public Task<List<RomMissingUpdates>> GetMissingUpdates()
    {
        throw new NotImplementedException();
    }

    public Task<RomMissingUpdates?> GetMissingUpdates(Rom rom)
    {
        throw new NotImplementedException();
    }

    public Task<List<RomMissingDLCs>> GetMissingDLC()
    {
        throw new NotImplementedException();
    }

    Task<RomMissingDLCs> IRomMissingContentChecker.GetMissingDLC(Rom rom)
    {
        throw new NotImplementedException();
    }

    public List<Rom> GetRomGroup(Rom rom)
    {
        throw new NotImplementedException();
    }
}
