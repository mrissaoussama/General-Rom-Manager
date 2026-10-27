using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Switch.Configuration;
using RomManagerShared.Switch.Parsers;
using RomManagerShared.Utils;
namespace RomManagerShared.Switch;

public class SwitchManager : ConsoleManager<SwitchConsole>, IMissingUpdateReporter, IMissingDlcReporter
{
    public List<List<Rom>> GroupedRomList { get; set; }

    // ── Manager-level requirements ────────────────────────────────────────
    private readonly ConsoleRequirement _titleDbDirReq = new()
    {
        Id          = "switch.titledb_dir",
        DisplayName = "TitleDB directory",
        Description = "Local directory containing Switch TitleDB JSON files.",
        Kind        = RequirementKind.DirectoryPath,
        DefaultValue = SwitchConfiguration.GetTitleDBPath(),
        IsOptional  = true,
    };
    private readonly ConsoleRequirement _versionsFileReq = new()
    {
        Id          = "switch.versions_json",
        DisplayName = "versions.json",
        Description = "Switch version database JSON file used for update checks.",
        Kind        = RequirementKind.DatabaseFile,
        DefaultValue = SwitchConfiguration.GetVersionsPath(),
        IsOptional  = true,
    };

    public override IReadOnlyList<ConsoleRequirement> GetAllRequirements()
    {
        var baseReqs = base.GetAllRequirements().ToList();
        baseReqs.Add(_titleDbDirReq);
        baseReqs.Add(_versionsFileReq);
        return baseReqs.DistinctBy(r => r.Id).ToList();
    }

    public SwitchManager(TitleInfoProviderManager<SwitchConsole> titleInfoProviderManager,
RomParserExecutor<SwitchConsole> romParserExecutor)
: base(romParserExecutor,titleInfoProviderManager)
    {
        GroupedRomList = [];
    }

    // ── IMissingUpdateReporter ────────────────────────────────────────────
    public bool HasVersionDatabase => File.Exists(SwitchConfiguration.GetVersionsPath());

    public Task DownloadVersionDatabaseAsync() => SwitchDownloads.DownloadVersionsFile(overwrite: true);

    public IReadOnlyList<MissingUpdate> FindMissingUpdates(IReadOnlyList<Rom> library)
        => SwitchUpdateChecker.FindMissing(library, SwitchUpdateChecker.LoadLatestVersions(SwitchConfiguration.GetVersionsPath()));

    // ── IMissingDlcReporter ───────────────────────────────────────────────
    public async Task<IReadOnlyList<(string Game, string Dlc, string TitleId)>> FindMissingDlcAsync(IReadOnlyList<Rom> library)
    {
        var titledb = TitleInfoProviderManager?.TitleInfoProviders.OfType<TitleInfoProviders.SwitchJsonTitleInfoProvider>().FirstOrDefault();
        if (titledb is null) return [];
        if (titledb.TitlesDatabase is null) await titledb.LoadTitleDatabaseAsync();
        if (titledb.TitlesDatabase is not { Count: > 0 } db) return [];
        return [.. SwitchDlcChecker.FindMissing(library, SwitchDlcChecker.Catalog(db))
            .Select(m => (m.GameTitle, m.Name ?? "(unnamed DLC)", m.TitleId))];
    }

    public void LoadGroupRomList()
    {
        GroupedRomList = SwitchUtils.GroupRomList(RomList);
    }
    public List<string> GetSupportedExtensions()
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

    // ── IHasContextMenuActions ────────────────────────────────────────────
    public override IEnumerable<IConsoleRomAction> GetContextMenuActions()
    {
        // Universal actions from base
        foreach (var action in base.GetContextMenuActions())
            yield return action;
        // Switch-specific actions
        yield return new SwitchGroupByTitleAction();
    }
}
