using RomManagerShared.Base;
using RomManagerShared.Base.Database;
using RomManagerShared.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace RomManagerShared.Base;

// ═══════════════════════════════════════════════════════════════════════════
// Universal context-menu actions backed by the shared utility classes.
// These are registered via IHasContextMenuActions on ConsoleManager<T> base.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Find duplicate ROMs by TitleID / Hash / Name.</summary>
public sealed class FindDuplicatesAction : ConsoleRomActionBase
{
    public override string Name => "🔍 Find Duplicates";
    public override string Category => "Organise";
    public override bool SupportsBulk => true;

    public override async Task ExecuteAsync(IEnumerable<Rom> selectedRoms, IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var detector = new DuplicateDetector(DuplicateStrategy.Default);
        var groups = detector.FindDuplicates(selectedRoms);
        if (groups.Count == 0)
        {
            AppLog.Info("[FindDuplicates] No duplicates found.");
            return;
        }

        foreach (var g in groups)
        {
            var names = string.Join(", ", g.Roms.Select(r =>
                r.Titles?.FirstOrDefault()?.Value ?? r.TitleID ?? "?"));
            AppLog.Info($"[FindDuplicates] Group '{g.GroupKey}' ({g.MatchedBy}): {names}");
        }

        AppLog.Info($"[FindDuplicates] Found {groups.Count} duplicate group(s), {groups.Sum(g => g.Redundant.Count)} redundant ROM(s).");
        await Task.CompletedTask;
    }
}

/// <summary>Rename selected ROMs using the format template from Settings.</summary>
public sealed class RenameRomsAction : ConsoleRomActionBase
{
    public override string Name => "✏️ Rename Files";
    public override string Category => "Organise";
    public override bool SupportsBulk => true;

    public override async Task ExecuteAsync(IEnumerable<Rom> selectedRoms, IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        // The format string is passed via a well-known service key
        var formatString = "{TitleName} [{Region}] [v{Version}]"; // default
        var results = await FileRenamer.RenameFilesAsync(selectedRoms, formatString, ct: cancellationToken);

        int ok = results.Count(r => r.Success && r.Changed);
        int skip = results.Count(r => !r.Changed);
        int fail = results.Count(r => !r.Success);

        AppLog.Warn($"[Rename] Done: {ok} renamed, {skip} unchanged, {fail} failed.");
    }
}

/// <summary>Calculate hashes (CRC32, MD5, SHA1) for selected ROMs and persist to DB.</summary>
public sealed class CalculateHashesAction : ConsoleRomActionBase
{
    public override string Name => "#️⃣ Calculate Hashes";
    public override string Category => "Tools";
    public override bool SupportsBulk => true;

    public override async Task ExecuteAsync(IEnumerable<Rom> selectedRoms, IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        RomHashRepository? repo = null;
        try { repo = services.GetService<RomHashRepository>(); }
        catch { /* no database in this host */ }
        await HashUtils.HashRomsAsync([.. selectedRoms], repo is null ? null : hashes => repo.AddIfNewRange(hashes), ct: cancellationToken);
    }
}
