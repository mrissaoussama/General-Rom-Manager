using Microsoft.EntityFrameworkCore;
using RomManagerShared.Utils;

namespace RomManagerShared.Base.Database;

public partial class AppDbContext
{
    // ── No-Intro DAT ──────────────────────────────────────────────────────
    public DbSet<NoIntroDataFile> NoIntroDataFiles { get; set; } = null!;
    public DbSet<NoIntroGame> NoIntroGames { get; set; } = null!;
    public DbSet<NoIntroHeader> NoIntroHeaders { get; set; } = null!;
    public DbSet<NoIntroRom> NoIntroRoms { get; set; } = null!;

    partial void ConfigureNoIntroModels(ModelBuilder modelBuilder)
    {
        // Future: unique index or other constraints on NoIntro tables
    }
}
