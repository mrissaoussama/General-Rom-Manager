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
            FileUtils.Log("[FindDuplicates] No duplicates found.");
            return;
        }

        foreach (var g in groups)
        {
            var names = string.Join(", ", g.Roms.Select(r =>
                r.Titles?.FirstOrDefault()?.Value ?? r.TitleID ?? "?"));
            FileUtils.Log($"[FindDuplicates] Group '{g.GroupKey}' ({g.MatchedBy}): {names}");
        }

        FileUtils.Log($"[FindDuplicates] Found {groups.Count} duplicate group(s), {groups.Sum(g => g.Redundant.Count)} redundant ROM(s).");
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

        FileUtils.Log($"[Rename] Done: {ok} renamed, {skip} unchanged, {fail} failed.");
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
        // Resolve DB repository from DI for hash persistence
        RomHashRepository? repo = null;
        try { repo = services.GetService<RomHashRepository>(); }
        catch { /* swallow if DI not configured */ }

        foreach (var rom in selectedRoms)
        {
            if (string.IsNullOrEmpty(rom.Path)) continue;
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var hashTypes = new[] { HashTypeEnum.CRC32, HashTypeEnum.MD5, HashTypeEnum.SHA1 };
                List<RomHash> hashes;

                if (rom.IsFolderFormat && Directory.Exists(rom.Path))
                    hashes = await HashUtils.CalculateRomFolderHashes(rom, hashTypes);
                else if (File.Exists(rom.Path))
                    hashes = await HashUtils.CalculateRomHashes(rom, hashTypes);
                else
                    continue;

                // Save to DB if repository is available
                if (repo is not null && hashes.Count > 0)
                {
                    try { await repo.AddIfNewRange(hashes); }
                    catch (Exception dbEx) { FileUtils.Log($"[Hash] DB save failed for '{rom.Path}': {dbEx.Message}"); }
                }

                var hashStr = rom.Hashes?.FirstOrDefault()?.Value ?? "(none)";
                FileUtils.Log($"[Hash] {rom.Titles?.FirstOrDefault()?.Value ?? rom.TitleID}: {hashStr}");
            }
            catch (Exception ex)
            {
                FileUtils.Log($"[Hash] Error for '{rom.Path}': {ex.Message}");
            }
        }
    }
}
