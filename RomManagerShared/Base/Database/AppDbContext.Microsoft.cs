using Microsoft.EntityFrameworkCore;
using RomManagerShared.OriginalXbox;
using RomManagerShared.Xbox360;

namespace RomManagerShared.Base.Database;

public partial class AppDbContext
{
    // ── Original Xbox ─────────────────────────────────────────────────────
    public DbSet<OriginalXboxGame> OriginalXboxGames { get; set; } = null!;

    // ── Xbox 360 ──────────────────────────────────────────────────────────
    public DbSet<Xbox360Game> Xbox360Games { get; set; } = null!;
    public DbSet<Xbox360DLC> Xbox360DLCs { get; set; } = null!;
    public DbSet<Xbox360Update> Xbox360Updates { get; set; } = null!;

    partial void ConfigureMicrosoftModels(ModelBuilder modelBuilder)
    {
        // Future: Microsoft-specific model constraints
    }
}
