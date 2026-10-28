using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RomManagerShared;
using RomManagerShared.Base;
using RomManagerShared.Base.Database;
using RomManagerShared.Base.Library;
using RomManagerShared.Interfaces;
using RomManagerShared.Switch;
using RomManagerShared.Utils;

namespace RomManagerTests;

/// <summary>The plugin context's dialogs, file rules, metadata writes and their undo, against a real SQLite library.</summary>
[Collection("PluginData")] // PluginManager's static folder overrides
public sealed class PluginApiTests : IDisposable
{
    private static readonly byte[] Png = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

    private readonly string _dir = Directory.CreateTempSubdirectory("plugin-api").FullName;
    private readonly string _db;
    private readonly ServiceProvider _services;
    private readonly FakePluginHost _host = new();

    public PluginApiTests()
    {
        _db = Path.Combine(_dir, "lib.db");
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={_db}"));
        _services = services.BuildServiceProvider();
        using (var scope = _services.CreateScope())
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
        PluginManager.CoversRootOverride = Path.Combine(_dir, "covers");
        PluginManager.DataRootOverride = Path.Combine(_dir, "data");
    }

    public void Dispose()
    {
        PluginManager.CoversRootOverride = null;
        PluginManager.DataRootOverride = null;
        _services.Dispose();
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    private DefaultPluginContext Context(IReadOnlyList<Rom> selected, params PluginPermission[] granted) =>
        new(_services, NullLogger.Instance, "Test", granted, Path.Combine(_dir, "data", "Test"), Path.Combine(_dir, "config.json"),
            selected, _host);

    private static SwitchGame Game(string id, string title, string path) =>
        new() { TitleID = id, Titles = [new RomTitle(title)], Path = path, Publisher = "Old publisher" };

    [Fact]
    public void SelectedRoms_AreCopies()
    {
        var rom = Game("0100", "Zelda", "/roms/zelda.nsp");
        using var ctx = Context([rom]);

        var copy = ctx.SelectedRoms.Should().ContainSingle().Subject;
        copy.Should().NotBeSameAs(rom).And.BeOfType<SwitchGame>();
        copy.Titles!.Clear();

        rom.Titles.Should().ContainSingle();
    }

    [Fact]
    public async Task UpdateMetadata_NeedsThePermission_SavesAnOverride_AndUndoPutsItBack()
    {
        var rom = Game("0100ABC", "Zelda", Path.Combine(_dir, "zelda.nsp"));
        using (var denied = Context([rom]))
            await denied.Awaiting(c => c.UpdateMetadataAsync(c.SelectedRoms[0], new RomMetadata { Publisher = "X" }))
                .Should().ThrowAsync<PluginPermissionException>();

        using var ctx = Context([rom], PluginPermission.LibraryMetadata);
        await ctx.UpdateMetadataAsync(ctx.SelectedRoms[0], new RomMetadata
        {
            Title = "The Legend of Zelda",
            Description = "An adventure",
            Publisher = "Nintendo",
            ReleaseDate = new DateOnly(2017, 3, 3),
            Genres = ["Action", "Role-playing (RPG)", "Nonsense"],
            CoverImage = Png,
        }, TestContext.Current.CancellationToken);

        // The library's rom changed in memory...
        rom.Titles![0].Value.Should().Be("The Legend of Zelda");
        rom.Publisher.Should().Be("Nintendo");
        rom.Genres.Should().Equal(GenreEnum.Action, GenreEnum.RolePlaying);
        File.ReadAllBytes(rom.Icon!).Should().Equal(Png);

        // ...and a fresh copy of it (a rescan) gets the same values from the stored override.
        var rescanned = Game("0100abc", "Zelda", Path.Combine(_dir, "moved", "zelda.nsp"));
        using (var scope = _services.CreateScope())
            await new MetadataOverrideStore(scope.ServiceProvider.GetRequiredService<AppDbContext>())
                .ApplyAsync(MetadataOverrideStore.ConsoleOf(rescanned), [rescanned], TestContext.Current.CancellationToken);
        rescanned.Titles![0].Value.Should().Be("The Legend of Zelda");
        rescanned.Descriptions![0].Value.Should().Be("An adventure");

        // Undo restores both.
        await using var manager = new PluginManager(_services, NullLoggerFactory.Instance);
        await manager.UndoMetadataAsync(ctx.Changes, TestContext.Current.CancellationToken);
        rom.Titles[0].Value.Should().Be("Zelda");
        rom.Publisher.Should().Be("Old publisher");
        using var check = _services.CreateScope();
        check.ServiceProvider.GetRequiredService<AppDbContext>().RomMetadataOverrides.Should().BeEmpty();
    }

    [Fact]
    public async Task Scanner_AppliesOverrides_ToParsedAndCachedRoms()
    {
        var root = Directory.CreateDirectory(Path.Combine(_dir, "roms")).FullName;
        File.WriteAllText(Path.Combine(root, "0100DEF.nsp"), "x");
        var manager = new ConsoleManager<SwitchConsole>(new RomParserExecutor<SwitchConsole>([new IdFromName()]));
        var ct = TestContext.Current.CancellationToken;

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var scanner = new LibraryScanner(db);
        var first = await scanner.ScanAsync(manager, nameof(SwitchConsole), root, ct: ct);
        await new MetadataOverrideStore(db).SetAsync(first[0], new RomMetadata { Title = "Named by a plugin" }, "Test", Path.Combine(_dir, "covers"), ct);

        (await scanner.ScanAsync(manager, nameof(SwitchConsole), root, fullRescan: true, ct: ct))[0].Titles![0].Value.Should().Be("Named by a plugin");
        (await scanner.LoadCachedAsync(nameof(SwitchConsole), ct))[0].Titles![0].Value.Should().Be("Named by a plugin");
    }

    private sealed class IdFromName : IRomParser<SwitchConsole>
    {
        public List<string> Extensions { get; set; } = ["nsp"];
        public Task<List<Rom>> ProcessFile(string path) =>
            Task.FromResult<List<Rom>>([new SwitchGame { TitleID = Path.GetFileNameWithoutExtension(path), Path = path, Titles = [new RomTitle("Parsed")] }]);
    }

    [Fact]
    public async Task APickedFile_IsWritableWithoutTheFileSystemPermission()
    {
        var outside = Path.Combine(_dir, "export", "list.lpl");
        Directory.CreateDirectory(Path.GetDirectoryName(outside)!);
        using var ctx = Context([]);

        ctx.Invoking(c => c.OpenFile(outside, FileMode.Create, FileAccess.Write)).Should().Throw<PluginPermissionException>();

        _host.SavePath = outside;
        (await ctx.PickSaveFileAsync("Save", "list.lpl", "lpl")).Should().Be(outside);
        using (var stream = ctx.OpenFile(outside, FileMode.Create, FileAccess.Write)) stream.WriteByte(1);
        File.Exists(outside).Should().BeTrue();
    }

    [Fact]
    public void OpenInBrowser_OnlyTakesWebAddresses()
    {
        using var ctx = Context([]);
        ctx.OpenInBrowser(new Uri("https://example.org/?q=x"));
        ctx.Invoking(c => c.OpenInBrowser(new Uri("file:///etc/passwd"))).Should().Throw<ArgumentException>();
        _host.Opened.Should().ContainSingle();
    }

    [Theory]
    [InlineData("Role-playing (RPG)", GenreEnum.RolePlaying)]
    [InlineData("Platformer", GenreEnum.Platformer)]
    [InlineData("Sports games", GenreEnum.Sports)]
    [InlineData("Massively Multiplayer", GenreEnum.Unknown)]
    [InlineData("5", GenreEnum.Unknown)]
    public void GenreNames_MapCommonSpellings(string name, GenreEnum expected) => GenreNames.Map(name).Should().Be(expected);
}
