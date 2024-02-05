//using Microsoft.EntityFrameworkCore;
//using RomManagerShared.Base;
//using RomManagerShared.DS;
//using RomManagerShared.GameBoy;
//using RomManagerShared.GameBoyAdvance;
//using RomManagerShared.PS4;
//using RomManagerShared.PS4;
//using RomManagerShared.PSP;
//using RomManagerShared.ThreeDS;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace RomManagerShared.Database
//{
//    public class AppDbContext : DbContext
//    {
//        public DbSet<PS4Game> PS4Games { get; set; }
//        public DbSet<PS4DLC> PS4DLCs { get; set; }
//        public DbSet<PS4Update> PS4Updates { get; set; }

//        public DbSet<PS4Game> PS4Games { get; set; }
//        public DbSet<PS4DLC> PS4DLCs { get; set; }
//        public DbSet<PS4Update> PS4Updates { get; set; }
//        public DbSet<PS5Game> PS5Games { get; set; }
//        public DbSet<PS5DLC> PS5DLCs { get; set; }
//        public DbSet<PS5Update> PS5Updates { get; set; }
//        public DbSet<PS2Game> PS2Games { get; set; }
//        public DbSet<PS3Game> PS3Games { get; set; }
//        public DbSet<PS3DLC> PS3DLCs { get; set; }
//        public DbSet<PS3Update> PS3Updates { get; set; }
//        public DbSet<ThreeDSGame> ThreeDSGames { get; set; }
//        public DbSet<ThreeDSDLC> ThreeDSDLCs { get; set; }
//        public DbSet<ThreeDSUpdate> ThreeDSUpdates { get; set; }
//        public DbSet<GameBoyAdvanceGame> GBAGames { get; set; }
//        public DbSet<GameBoyGame> GBGames { get; set; }
//        public DbSet<DSGame> DSGames { get; set; }
//        public DbSet<PSPGame> PSPGames { get; set; }
//        public DbSet<PS4DLC> PS4DLCs { get; set; }
//        public DbSet<PS4Update> PS4Updates { get; set; }
//        public DbSet<PS4Game> PS4Games { get; set; }
//        public DbSet<PS4DLC> PS4DLCs { get; set; }
//        public DbSet<PS4Update> PS4Updates { get; set; }
//        public string DbPath { get; }

//        public AppDbContext()
//        {

//            DbPath = RomManagerConfiguration.GetSqliteDBPath();
//        }

//        // The following configures EF to create a Sqlite database file in the
//        // special "local" folder for your platform.
//        protected override void OnConfiguring(DbContextOptionsBuilder options)
//            => options.UseSqlite($"Data Source={DbPath}");
//    }

//}
