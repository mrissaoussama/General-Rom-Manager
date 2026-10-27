using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.PS4.Parsers;
using RomManagerShared.Utils;
namespace RomManagerShared.PS4;

public class PS4Manager : ConsoleManager<PS4Console>, IRomMissingContentChecker, IMissingUpdateReporter
{
 
    public List<List<Rom>> GroupedRomList { get; set; }
    PS4PKGUpdateAndDLCChecker Checker { get; set; }
    public PS4Manager(
RomParserExecutor<PS4Console> romParserExecutor)
: base(romParserExecutor)
    {
        GroupedRomList=[]; Checker = new();
    }
    // ── IMissingUpdateReporter: Sony's update server, opt-in ──────────────
    public bool HasVersionDatabase => PS4UpdateChecker.OnlineAllowed;

    public string SetupPrompt =>
        "Checking PS4 updates asks Sony's update server about each game in the library (one request per game). Allow it for this session? (Settings can allow it permanently.)";

    public bool IsOnline => true;

    public Task DownloadVersionDatabaseAsync()
    {
        PS4UpdateChecker.OnlineAllowed = true;
        return Task.CompletedTask;
    }

    public IReadOnlyList<MissingUpdate> FindMissingUpdates(IReadOnlyList<Rom> library) =>
        FindMissingUpdatesAsync(library).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<MissingUpdate>> FindMissingUpdatesAsync(IReadOnlyList<Rom> library, CancellationToken ct = default)
    {
        if (!PS4UpdateChecker.OnlineAllowed) return [];
        using var http = PS4UpdateChecker.CreateClient();
        return await PS4UpdateChecker.FindMissingAsync(library, (url, token) => PS4UpdateChecker.FetchAsync(http, url, token), ct);
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
            .OrderByDescending(rom => Version.TryParse(((PS4Update)rom).Version, out var v) ? v : new Version())
            .First();
        return latestUpdate;
    }

    public Task<List<RomMissingUpdates>> GetMissingUpdates()
    {
        AppLog.Warn("[PS4] GetMissingUpdates not yet implemented");
        return Task.FromResult(new List<RomMissingUpdates>());
    }

    public Task<RomMissingUpdates?> GetMissingUpdates(Rom rom)
    {
        AppLog.Warn("[PS4] GetMissingUpdates(rom) not yet implemented");
        return Task.FromResult<RomMissingUpdates?>(null);
    }

    public Task<List<RomMissingDLCs>> GetMissingDLC()
    {
        AppLog.Warn("[PS4] GetMissingDLC not yet implemented");
        return Task.FromResult(new List<RomMissingDLCs>());
    }

    Task<RomMissingDLCs> IRomMissingContentChecker.GetMissingDLC(Rom rom)
    {
        AppLog.Warn("[PS4] GetMissingDLC(rom) not yet implemented");
        return Task.FromResult(new RomMissingDLCs());
    }

    public List<Rom> GetRomGroup(Rom rom)
    {
        LoadGroupRomList();
        var group = GroupedRomList.Where(g => g.Contains(rom))
            .SelectMany(g => g).ToList();
        return group;
    }
}
