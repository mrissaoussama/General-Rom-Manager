using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RomManagerShared.Base.Database;

namespace RomManagerTests;

public sealed class DatabaseStartupTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_dir, true);
    }

    private AppDbContext Create(string path) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={path}").Options);

    [Fact]
    public void AHealthyDatabase_IsMigratedInPlace()
    {
        var path = Path.Combine(_dir, "lib.db");

        DatabaseStartup.MigrateOrReset(() => Create(path), path).Should().BeNull();
        DatabaseStartup.MigrateOrReset(() => Create(path), path).Should().BeNull();

        using var db = Create(path);
        db.LibraryFiles.Count().Should().Be(0);
    }

    [Fact]
    public void ACorruptDatabase_IsMovedAsideAndRecreated()
    {
        var path = Path.Combine(_dir, "lib.db");
        File.WriteAllText(path, "this is not a SQLite database, just text that fills the header and more");

        var aside = DatabaseStartup.MigrateOrReset(() => Create(path), path);

        aside.Should().NotBeNull();
        File.ReadAllText(aside!).Should().StartWith("this is not a SQLite database");
        using var db = Create(path);
        db.LibraryFiles.Count().Should().Be(0);
    }
}
