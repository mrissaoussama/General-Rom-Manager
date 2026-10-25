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
        var processedhash = await RomParserExecutor.ExecuteParsers(file);
        if (RomParserExecutor.Parsers.Count == 0)
        {
           // processedhash= await IdentifyRomFromHash(file);
        }
        var processedlist = processedhash.ToList();
        RomList.AddRange(processedlist);
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

    // ── Universal context menu actions (all consoles) ─────────────────────
    /// <summary>
    /// Returns the universal set of context actions available for every console.
    /// Subclasses can override to add console-specific actions.
    /// </summary>
    public virtual IEnumerable<IConsoleRomAction> GetContextMenuActions()
    {
        yield return new FindDuplicatesAction();
        yield return new RenameRomsAction();
        yield return new CalculateHashesAction();
    }
}