using Microsoft.EntityFrameworkCore;
using RomManagerShared.DS;
using RomManagerShared.GameBoy;
using RomManagerShared.GameBoyAdvance;
using RomManagerShared.Nintendo64;
using RomManagerShared.SNES;
using RomManagerShared.Switch;
using RomManagerShared.Switch.TitleInfoProviders;
using RomManagerShared.ThreeDS;
using RomManagerShared.ThreeDS.TitleInfoProviders;
using RomManagerShared.Wii;
using RomManagerShared.Wii.TitleInfoProviders;
using RomManagerShared.WiiU;
using static RomManagerShared.ThreeDS.TitleInfoProviders.ThreeDSJsonTitleInfoProvider;

namespace RomManagerShared.Base.Database;

public partial class AppDbContext
{
    // ── Nintendo 3DS ──────────────────────────────────────────────────────
    public DbSet<ThreeDSGame> ThreeDSGames { get; set; } = null!;
    public DbSet<ThreeDSDLC> ThreeDSDLCs { get; set; } = null!;
    public DbSet<ThreeDSUpdate> ThreeDSUpdates { get; set; } = null!;
    public DbSet<ThreeDSJsonDTO> ThreeDSJsonDTOs { get; set; } = null!;

    // ── Nintendo DS ───────────────────────────────────────────────────────
    public DbSet<DSGame> DSGames { get; set; } = null!;

    // ── Game Boy family ───────────────────────────────────────────────────
    public DbSet<GameBoyGame> GBGames { get; set; } = null!;
    public DbSet<GameBoyAdvanceGame> GBAGames { get; set; } = null!;

    // ── Nintendo 64 ───────────────────────────────────────────────────────
    public DbSet<Nintendo64Game> N64Games { get; set; } = null!;

    // ── SNES ──────────────────────────────────────────────────────────────
    public DbSet<SNESGame> SNESGames { get; set; } = null!;

    // ── Nintendo Switch ───────────────────────────────────────────────────
    public DbSet<SwitchGame> SwitchGames { get; set; } = null!;
    public DbSet<SwitchDLC> SwitchDLCs { get; set; } = null!;
    public DbSet<SwitchUpdate> SwitchUpdates { get; set; } = null!;
    public DbSet<SwitchJsonRomDTO> SwitchJsonRomDTOs { get; set; } = null!;

    // ── Nintendo Wii ──────────────────────────────────────────────────────
    public DbSet<WiiGame> WiiGames { get; set; } = null!;
    public DbSet<WiiGameTDBXmlRomDTO> WiiXmlRomDTOs { get; set; } = null!;

    // ── Nintendo Wii U ────────────────────────────────────────────────────
    public DbSet<WiiUGame> WiiUGames { get; set; } = null!;
    public DbSet<WiiUDLC> WiiUDLCs { get; set; } = null!;
    public DbSet<WiiUUpdate> WiiUUpdates { get; set; } = null!;
    public DbSet<WiiUWikiBrewTitleDTO> WiiUWikiBrewTitleDTOs { get; set; } = null!;

    partial void ConfigureNintendoModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ThreeDSJsonDTO>()
            .HasIndex(e => e.TitleID).IsUnique();

        modelBuilder.Entity<SwitchJsonRomDTO>()
            .HasIndex(e => e.TitleID).IsUnique();

        modelBuilder.Entity<WiiGameTDBXmlRomDTO>()
            .HasIndex(e => e.TitleID).IsUnique();
    }
}
