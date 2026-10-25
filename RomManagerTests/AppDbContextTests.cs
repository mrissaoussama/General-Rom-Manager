using FluentAssertions;
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

    // ── Basic CRUD ────────────────────────────────────────────────────────

    [Fact]
    public async Task CanInsertAndQueryRom()
    {
        await using var ctx = CreateInMemory(nameof(CanInsertAndQueryRom));

        var game = new Game
        {
            TitleID = "01004D300C5AE000",
            Version = "1.0",
            Titles = [new RomTitle("Breath of the Wild")]
        };

        ctx.Roms.Add(game);
        await ctx.SaveChangesAsync();

        var loaded = await ctx.Roms
            .FirstOrDefaultAsync(r => r.TitleID == "01004D300C5AE000");

        loaded.Should().NotBeNull();
        loaded.Should().BeOfType<Game>();
    }

    [Fact]
    public async Task CanInsertMultipleRomTypes()
    {
        await using var ctx = CreateInMemory(nameof(CanInsertMultipleRomTypes));

        ctx.Roms.AddRange(
            new Game   { TitleID = "GAME01", Version = "1.0" },
            new Update { TitleID = "GAME01", Version = "2.0" },
            new DLC    { TitleID = "GAME01DLC" }
        );

        await ctx.SaveChangesAsync();
        var allRoms = await ctx.Roms.ToListAsync();
        allRoms.Should().HaveCount(3);
        allRoms.OfType<Game>().Should()  .HaveCount(1);
        allRoms.OfType<Update>().Should().HaveCount(1);
        allRoms.OfType<DLC>().Should()  .HaveCount(1);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsData()
    {
        await using var ctx = CreateInMemory(nameof(SaveChangesAsync_PersistsData));

        ctx.Roms.Add(new Game { TitleID = "UNIQUE001" });
        int rows = await ctx.SaveChangesAsync();

        rows.Should().BeGreaterThan(0);
    }

    // ── IUnitOfWork ───────────────────────────────────────────────────────

    [Fact]
    public async Task IUnitOfWork_SaveChangesAsync_Works()
    {
        await using var ctx = CreateInMemory(nameof(IUnitOfWork_SaveChangesAsync_Works));
        IUnitOfWork uow = ctx;

        ctx.Roms.Add(new Game { TitleID = "UOW001" });
        int changes = await uow.SaveChangesAsync();

        changes.Should().BeGreaterThan(0);
    }

    // ── Discriminator (TPH) ───────────────────────────────────────────────

    [Fact]
    public async Task Discriminator_AllowsPolymorphicQuery()
    {
        await using var ctx = CreateInMemory(nameof(Discriminator_AllowsPolymorphicQuery));

        ctx.Roms.Add(new Game   { TitleID = "POLY_GAME" });
        ctx.Roms.Add(new Update { TitleID = "POLY_UPDATE" });
        await ctx.SaveChangesAsync();

        var games = await ctx.Roms.OfType<Game>().ToListAsync();
        games.Should().HaveCount(1);
        games[0].TitleID.Should().Be("POLY_GAME");
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
