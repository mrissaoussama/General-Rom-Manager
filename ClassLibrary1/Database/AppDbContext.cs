using Microsoft.EntityFrameworkCore;
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
using RomManagerShared.ThreeDS;
using RomManagerShared.Wii;
using RomManagerShared.WiiU;

namespace RomManagerShared.Database
{
    public class AppDbContext : DbContext
    {
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
        //public DbSet<PS5DLC> PS5DLCs { get; set; }
        //public DbSet<PS5Update> PS5Updates { get; set; }
        //public DbSet<PS5Game> PS5Updates { get; set; }

        public string DbPath { get; }

        public AppDbContext()
        {

            DbPath = RomManagerConfiguration.GetSqliteDBPath();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");
    }

}
