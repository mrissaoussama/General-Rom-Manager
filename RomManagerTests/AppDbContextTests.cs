using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using RomManagerShared.Base;
using RomManagerShared.Base.Database;

namespace RomManagerTests;

/// <summary>
/// Tests AppDbContext with an in-memory database.
/// Validates that the EF model is set up correctly and UoW operations work.
/// </summary>
public class AppDbContextTests
{
    // ── Factory ───────────────────────────────────────────────────────────

    private static AppDbContext CreateInMemory(string dbName = "TestDb")
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .EnableSensitiveDataLogging()
            .Options;

        return new AppDbContext(options);
    }

    // ── Model ─────────────────────────────────────────────────────────────

    // Console modules can be added or removed, so the model holds nothing console-specific.
    [Fact]
    public void Model_HasOnlyCoreTables()
    {
        using var db = CreateInMemory(Guid.NewGuid().ToString());
        db.Model.GetEntityTypes().Select(e => e.ClrType.Name).Should()
            .BeEquivalentTo(["RomHash", "RomHashProperty", "LibraryFile"]);
    }

    [Fact]
    public async Task IUnitOfWork_SaveChangesAsync_PersistsLibraryRows()
    {
        var name = Guid.NewGuid().ToString();
        await using (var db = CreateInMemory(name))
        {
            IUnitOfWork uow = db;
            db.LibraryFiles.Add(new RomManagerShared.Base.Library.LibraryFile { Console = "NES", Path = "/a.nes", RomsJson = "[]" });
            await uow.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using var again = CreateInMemory(name);
        again.LibraryFiles.Should().ContainSingle().Which.Path.Should().Be("/a.nes");
    }

    // ── DbPath fallback ───────────────────────────────────────────────────

    [Fact]
    public void GetDbPath_DoesNotThrowWhenConfigNotLoaded()
    {
        // Config may not be loaded in test environment — should fall back gracefully
        var act = () => AppDbContext.GetDbPath();
        act.Should().NotThrow();
    }
}
