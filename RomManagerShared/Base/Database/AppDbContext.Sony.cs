using Microsoft.EntityFrameworkCore;
using RomManagerShared.PS2;
using RomManagerShared.PS3;
using RomManagerShared.PS4;
using RomManagerShared.PSP;
using RomManagerShared.PSVita;
using RomManagerShared.SegaSaturn;

namespace RomManagerShared.Base.Database;

public partial class AppDbContext
{
    // ── PlayStation 2 ─────────────────────────────────────────────────────
    public DbSet<PS2Game> PS2Games { get; set; } = null!;

    // ── PlayStation 3 ─────────────────────────────────────────────────────
    public DbSet<PS3Game> PS3Games { get; set; } = null!;
    public DbSet<PS3DLC> PS3DLCs { get; set; } = null!;
    public DbSet<PS3Update> PS3Updates { get; set; } = null!;

    // ── PlayStation 4 ─────────────────────────────────────────────────────
    public DbSet<PS4Game> PS4Games { get; set; } = null!;
    public DbSet<PS4DLC> PS4DLCs { get; set; } = null!;
    public DbSet<PS4Update> PS4Updates { get; set; } = null!;

    // ── PlayStation Portable ──────────────────────────────────────────────
    public DbSet<PSPGame> PSPGames { get; set; } = null!;
    public DbSet<PSPDLC> PSPDLCs { get; set; } = null!;
    public DbSet<PSPUpdate> PSPUpdates { get; set; } = null!;

    // ── PlayStation Vita ──────────────────────────────────────────────────
    public DbSet<PSVitaGame> PSVitaGames { get; set; } = null!;
    public DbSet<PSVitaDLC> PSVitaDLCs { get; set; } = null!;
    public DbSet<PSVitaUpdate> PSVitaUpdates { get; set; } = null!;

    // ── Sega Saturn ───────────────────────────────────────────────────────
    public DbSet<SegaSaturnGame> SegaSaturnGames { get; set; } = null!;

    partial void ConfigureSonyModels(ModelBuilder modelBuilder)
    {
        // Future: Sony-specific model constraints
    }
}
