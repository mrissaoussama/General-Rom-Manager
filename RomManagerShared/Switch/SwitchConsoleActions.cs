using Microsoft.Extensions.DependencyInjection;
using RomManagerShared.Base;
using RomManagerShared.Utils;

namespace RomManagerShared.Switch;

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
