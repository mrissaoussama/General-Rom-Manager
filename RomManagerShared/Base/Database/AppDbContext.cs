using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore.Proxies;
using RomManagerShared.Base;
using RomManagerShared.Configuration;
using RomManagerShared.DS;
using RomManagerShared.GameBoy;
using RomManagerShared.GameBoyAdvance;
using RomManagerShared.Nintendo64;
using RomManagerShared.OriginalXbox;
using RomManagerShared.PS2;
using RomManagerShared.PS3;
using RomManagerShared.PS4;
using RomManagerShared.PSP;
using RomManagerShared.PSVita;
using RomManagerShared.SegaSaturn;
using RomManagerShared.SNES;
using RomManagerShared.Switch;
using RomManagerShared.Switch.TitleInfoProviders;
using RomManagerShared.ThreeDS;
using RomManagerShared.ThreeDS.TitleInfoProviders;
using RomManagerShared.Wii;
using RomManagerShared.Wii.TitleInfoProviders;
using RomManagerShared.WiiU;
using RomManagerShared.Xbox360;
using static RomManagerShared.ThreeDS.TitleInfoProviders.ThreeDSJsonTitleInfoProvider;
using RomManagerShared.Utils;

namespace RomManagerShared.Base.Database;

public class AppDbContext : DbContext
{
    public DbSet<Rom> Roms { get; set; }
    public DbSet<ThreeDSGame> ThreeDSGames { get; set; }
    public DbSet<ThreeDSDLC> ThreeDSDLCs { get; set; }
    public DbSet<ThreeDSUpdate> ThreeDSUpdates { get; set; }
    public DbSet<DSGame> DSGames { get; set; }
    public DbSet<GameBoyAdvanceGame> GBAGames { get; set; }
    public DbSet<GameBoyGame> GBGames { get; set; }
    public DbSet<Nintendo64Game> N64Games { get; set; }
    public DbSet<OriginalXboxGame> OriginalXboxGames { get; set; }
    public DbSet<PS2Game> PS2Games { get; set; }
    public DbSet<PS3Game> PS3Games { get; set; }
    public DbSet<PS3DLC> PS3DLCs { get; set; }
    public DbSet<PS3Update> PS3Updates { get; set; }
    public DbSet<PS4Game> PS4Games { get; set; }
    public DbSet<PS4DLC> PS4DLCs { get; set; }
    public DbSet<PS4Update> PS4Updates { get; set; }
    public DbSet<PSPGame> PSPGames { get; set; }
    public DbSet<PSPDLC> PSPDLCs { get; set; }
    public DbSet<PSPUpdate> PSPUpdates { get; set; }
    public DbSet<PSVitaGame> PSVitaGames { get; set; }
    public DbSet<PSVitaDLC> PSVitaDLCs { get; set; }
    public DbSet<PSVitaUpdate> PSVitaUpdates { get; set; }
    public DbSet<SegaSaturnGame> SegaSaturnGames { get; set; }
    public DbSet<SNESGame> SNESGames { get; set; }
    public DbSet<SwitchGame> SwitchGames { get; set; }
    public DbSet<SwitchDLC> SwitchDLCs { get; set; }
    public DbSet<SwitchUpdate> SwitchUpdates { get; set; }
    public DbSet<WiiGame> WiiGames { get; set; }
    public DbSet<WiiUGame> WiiUGames { get; set; }
    public DbSet<WiiUDLC> WiiUDLCs { get; set; }
    public DbSet<WiiUUpdate> WiiUUpdates { get; set; }
    public DbSet<Xbox360Game> Xbox360Games { get; set; }
    public DbSet<Xbox360DLC> Xbox360DLCs { get; set; }
    public DbSet<Xbox360Update> Xbox360Updates { get; set; }
    //public DbSet<PS5DLC> PS5DLCs { get; set; }
    //public DbSet<PS5Update> PS5Updates { get; set; }
    //public DbSet<PS5Game> PS5Updates { get; set; }

    public DbSet<WiiUWikiBrewTitleDTO> WiiUWikiBrewTitleDTOs { get; set; }
    public DbSet<ThreeDSJsonDTO> ThreeDSJsonDTOs { get; set; }
    public DbSet<SwitchJsonRomDTO> SwitchJsonRomDTOs { get; set; }
    public DbSet<WiiGameTDBXmlRomDTO> WiiXmlRomDTOs { get; set; }
    public DbSet<Rating> Ratings { get; set; }
    public DbSet<RomHash> RomHashes { get; set; }
    public DbSet<RomHashProperty> RomHashProperties { get; set; }
    public DbSet<NoIntroDataFile> NoIntroDataFiles { get; set; }
    public DbSet<NoIntroGame> NoIntroGames { get; set; }
    public DbSet<NoIntroHeader> NoIntroHeaders { get; set; }
    public DbSet<NoIntroRom> NoIntroRoms { get; set; }

    public string DbPath { get; set; }

    public AppDbContext()
    {
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        DbPath= Path.Combine(appDataFolder, "GeneralRomManager\\RomDB.sqlite");
        options.UseSqlite($"Data Source={DbPath}");
        options.UseLazyLoadingProxies();
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DLC>()
            .HasOne(dlc => dlc.RelatedGame)
            .WithMany(game => game.DLCs)
            .HasForeignKey(dlc => dlc.Id);

        modelBuilder.Entity<Update>()
            .HasOne(update => update.RelatedGame)
            .WithMany(game => game.Updates)
            .HasForeignKey(update => update.Id);
       // modelBuilder.Entity<WiiUWikiBrewTitleDTO>()
       //.HasIndex(e => e.ProductCode) 
       //.IsUnique(); 
        modelBuilder.Entity<ThreeDSJsonDTO>()
       .HasIndex(e => e.TitleID)
       .IsUnique(); 
        modelBuilder.Entity<SwitchJsonRomDTO>()
       .HasIndex(e => e.TitleID)
       .IsUnique();
        modelBuilder.Entity<WiiGameTDBXmlRomDTO>()
       .HasIndex(e => e.TitleID)
       .IsUnique();
      
        base.OnModelCreating(modelBuilder);
    }
}
