using RomManagerShared.Base;
using RomManagerShared.Utils;

namespace RomManagerShared.Interfaces;

public interface IConsoleManager<T>  where T : GamingConsole
{
    RomParserExecutor<T> RomParserExecutor { get; set; }
    List<Rom> RomList { get; set; }
    Task ProcessFile(string file);
    Task Setup();
}
public  class ConsoleManager<T> :IConsoleManager<T>, IHasContextMenuActions where T : GamingConsole
{
    public TitleInfoProviderManager<T>? TitleInfoProviderManager { get; set; }
    public RomParserExecutor<T> RomParserExecutor { get; set; }
    public List<Rom> RomList { get; set; }

    public ConsoleManager(RomParserExecutor<T> romParserExecutor, TitleInfoProviderManager<T> titleInfoProviderManager)
    {
        TitleInfoProviderManager = titleInfoProviderManager;
        RomList = [];
        RomParserExecutor = romParserExecutor;
    }
    public ConsoleManager(RomParserExecutor<T> romParserExecutor)
    {
        RomList = [];
        RomParserExecutor = romParserExecutor;
    }

    public virtual async Task ProcessFile(string file)
    {
        var roms = await ParseFile(file);
        RomList.AddRange(roms);
    }

    /// <summary>Parses and enriches one file without touching <see cref="RomList"/>.</summary>
    public virtual async Task<List<Rom>> ParseFile(string file)
        => await EnrichAsync(await RomParserExecutor.ExecuteParsers(file));

    /// <summary>Runs title info providers over parsed roms. Not thread-safe (providers may share a DbContext).</summary>
    public virtual async Task<List<Rom>> EnrichAsync(List<Rom> roms, CancellationToken ct = default)
    {
        if (TitleInfoProviderManager is not null)
        {
            for (int i = 0; i < roms.Count; i++)
                roms[i] = await TitleInfoProviderManager.GetTitleInfo(roms[i], ct);
        }
        return roms;
    }

    public virtual Task IdentifyRomFromHash(string file)
    {
        return Task.CompletedTask;
    }


    public virtual async Task Setup()
    {
        if(TitleInfoProviderManager is not null)
        await TitleInfoProviderManager.Setup();
    }

    // ── Requirement aggregation ───────────────────────────────────────────
    /// <summary>
    /// Collects all <see cref="ConsoleRequirement"/>s from parsers and
    /// title-info providers that implement <see cref="IHasRequirements"/>.
    /// Override in subclasses to add manager-level requirements.
    /// </summary>
    public virtual IReadOnlyList<ConsoleRequirement> GetAllRequirements()
    {
        var reqs = new List<ConsoleRequirement>();
        foreach (var parser in RomParserExecutor.Parsers)
            if (parser is IHasRequirements hr) reqs.AddRange(hr.GetRequirements());
        // Deduplicate by Id
        return reqs.DistinctBy(r => r.Id).ToList();
    }

    /// <summary>Non-optional requirements that are currently unmet.</summary>
    public IReadOnlyList<ConsoleRequirement> GetUnmetRequirements()
        => GetAllRequirements().Where(r => !r.IsOptional && !r.IsMet).ToList();

    // ── Console-specific context menu actions ─────────────────────────────
    /// <summary>
    /// Actions this console adds to the rom context menu, after the built-in ones (rename, move, hashes…).
    /// None by default; a module overrides this to add its own.
    /// </summary>
    public virtual IEnumerable<IConsoleRomAction> GetContextMenuActions() => [];
}