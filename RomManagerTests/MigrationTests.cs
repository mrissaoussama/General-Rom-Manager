using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RomManagerShared.Base.Database;

namespace RomManagerTests;

public class MigrationTests
{
    [Fact]
    public void Migrate_FreshDatabase_CreatesSchemaWithoutPendingChanges()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={dbPath}").Options;
            using (var db = new AppDbContext(options))
            {
                db.Database.Migrate();

                db.Database.GetPendingMigrations().Should().BeEmpty();
                db.Database.HasPendingModelChanges().Should().BeFalse();
                db.Roms.Count().Should().Be(0);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(dbPath);
        }
    }
}
