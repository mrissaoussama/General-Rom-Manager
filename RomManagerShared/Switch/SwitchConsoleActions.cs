using Microsoft.Extensions.DependencyInjection;
using RomManagerShared.Base;
using RomManagerShared.Utils;

namespace RomManagerShared.Switch;

// ── Check for Missing Updates ────────────────────────────────────────────────

internal sealed class SwitchCheckMissingUpdatesAction : ConsoleRomActionBase
{
    public override string Name => "Check Missing Updates";
    public override string Description => "Queries the online version database and reports any missing updates for selected games.";
    public override string Category => "Updates";
    public override string? IconKey => "icon_update";
    public override bool SupportsBulk => true;

    public override bool CanExecute(IEnumerable<Rom> selectedRoms)
        => selectedRoms.OfType<SwitchGame>().Any();

    public override async Task ExecuteAsync(IEnumerable<Rom> selectedRoms, IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var manager = services.GetRequiredService<SwitchManager>();
        await manager.GetMissingUpdates();
    }
}

// ── Group By Title ID ────────────────────────────────────────────────────────

internal sealed class SwitchGroupByTitleAction : ConsoleRomActionBase
{
    public override string Name => "Group ROMs by Title";
    public override string Description => "Groups the loaded ROM list by Title ID (Base + Updates + DLC).";
    public override string Category => "Organise";
    public override bool SupportsBulk => true;

    public override async Task ExecuteAsync(IEnumerable<Rom> selectedRoms, IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var manager = services.GetRequiredService<SwitchManager>();
        await Task.Run(manager.LoadGroupRomList, cancellationToken);
    }
}
