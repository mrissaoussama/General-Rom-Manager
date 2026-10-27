using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace RomManagerShared.Base.Database;

public static class DatabaseStartup
{
    /// <summary>
    /// Creates or upgrades the library database. The database is a cache of scan results and hashes, so when the
    /// migration fails (a corrupt file, a schema from a newer build) the file is moved aside as
    /// <c>&lt;name&gt;.broken-&lt;time&gt;</c> and a new one is created. Returns where the old file went, or null.
    /// </summary>
    public static string? MigrateOrReset(Func<AppDbContext> create, string dbPath)
    {
        try
        {
            using var db = create();
            db.Database.Migrate();
            return null;
        }
        catch (Exception ex)
        {
            AppLog.Error($"Database migration failed for '{dbPath}'", ex);
            if (!File.Exists(dbPath)) throw;
        }

        SqliteConnection.ClearAllPools(); // release the file before moving it
        var aside = $"{dbPath}.broken-{DateTime.Now:yyyyMMdd-HHmmss}";
        File.Move(dbPath, aside);
        foreach (var suffix in new[] { "-wal", "-shm" })
            if (File.Exists(dbPath + suffix)) File.Move(dbPath + suffix, aside + suffix);
        AppLog.Warn($"The library database was moved to '{aside}' and a new one is created; the next scan rebuilds it.");

        using (var db = create())
            db.Database.Migrate();
        return aside;
    }
}
