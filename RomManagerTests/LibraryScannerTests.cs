using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RomManagerShared;
using RomManagerShared.Base;
using RomManagerShared.Base.Database;
using RomManagerShared.Base.Library;
using RomManagerShared.Interfaces;
using RomManagerShared.PSP;

using RomManagerShared.Switch;

namespace RomManagerTests;

public sealed class LibraryScannerTests : IDisposable
{
    private sealed class CountingParser : IRomParser<SwitchConsole>
    {
        public int Calls;
        public List<string> Extensions { get; set; } = ["nsp"];
        public Task<List<Rom>> ProcessFile(string path)
        {
            var call = Interlocked.Increment(ref Calls); // the scanner parses files in parallel
            var rom = new Game { TitleID = Path.GetFileNameWithoutExtension(path), Path = path };
            rom.AddTitleName($"Title {call}");
            return Task.FromResult<List<Rom>>([rom]);
        }
    }

    private readonly string _root = Directory.CreateTempSubdirectory().FullName;
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
    private readonly AppDbContext _db;
    private readonly CountingParser _parser = new();
    private readonly ConsoleManager<SwitchConsole> _manager;

    public LibraryScannerTests()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={_dbPath}").Options);
        _db.Database.Migrate();
        _manager = new ConsoleManager<SwitchConsole>(new RomParserExecutor<SwitchConsole>([_parser]));
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
        Directory.Delete(_root, true);
    }

    [Fact]
    public async Task ScanAsync_SeveralRoots_FormOneLibrary_AndAnOfflineRootKeepsItsRows()
    {
        var second = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(_root, "A.nsp"), "a");
            File.WriteAllText(Path.Combine(second, "B.nsp"), "b");
            var scanner = new LibraryScanner(_db);
            var ct = TestContext.Current.CancellationToken;

            var roms = await scanner.ScanAsync(_manager, "Switch", [_root, second], false, null, ct, _ => ScanOptions.Default);
            roms.Select(r => r.TitleID).Should().BeEquivalentTo(["A", "B"]);

            // The second drive goes offline: B stays (from the cache), nothing is re-parsed.
            var offline = second + "-offline";
            Directory.Move(second, offline);
            try
            {
                roms = await scanner.ScanAsync(_manager, "Switch", [_root, second], false, null, ct, _ => ScanOptions.Default);
                roms.Select(r => r.TitleID).Should().BeEquivalentTo(["A", "B"]);
                _db.LibraryFiles.Count().Should().Be(2);
                _parser.Calls.Should().Be(2);
            }
            finally { Directory.Move(offline, second); }

            // Removed from the folder list (not offline): its rows go.
            roms = await scanner.ScanAsync(_manager, "Switch", [_root], false, null, ct, _ => ScanOptions.Default);
            roms.Should().ContainSingle().Which.TitleID.Should().Be("A");
        }
        finally { Directory.Delete(second, true); }
    }

    private sealed class DeletingParser(string victim) : IRomParser<SwitchConsole>
    {
        public List<string> Extensions { get; set; } = ["nsp"];
        public Task<List<Rom>> ProcessFile(string path)
        {
            if (path == victim) File.Delete(path); // e.g. moved away while the scan runs
            return Task.FromResult<List<Rom>>([new Game { TitleID = Path.GetFileNameWithoutExtension(path), Path = path }]);
        }
    }

    [Fact]
    public async Task ScanAsync_AFileThatDisappearsMidScanIsSkipped()
    {
        var a = Path.Combine(_root, "A.nsp");
        var b = Path.Combine(_root, "B.nsp");
        File.WriteAllText(a, "a");
        File.WriteAllText(b, "b");
        var manager = new ConsoleManager<SwitchConsole>(new RomParserExecutor<SwitchConsole>([new DeletingParser(b)]));

        var roms = await new LibraryScanner(_db).ScanAsync(manager, "Switch", _root, ct: TestContext.Current.CancellationToken);

        roms.Should().ContainSingle().Which.TitleID.Should().Be("A");
        _db.LibraryFiles.Select(f => f.Path).Should().Equal(a);
    }

    [Fact]
    public async Task ScanAsync_SkipsExcludedFolders()
    {
        File.WriteAllText(Path.Combine(_root, "A.nsp"), "a");
        Directory.CreateDirectory(Path.Combine(_root, "Backups"));
        File.WriteAllText(Path.Combine(_root, "Backups", "B.nsp"), "b");

        var roms = await new LibraryScanner(_db).ScanAsync(_manager, "Switch", _root, ct: TestContext.Current.CancellationToken,
            options: ScanOptions.For(_root, 1, ["Backups"]));

        roms.Should().ContainSingle().Which.TitleID.Should().Be("A");
        _parser.Calls.Should().Be(1);
    }

    [Fact]
    public async Task ScanAsync_ReusesUnchangedFiles_ReparsesChanged_RemovesDeleted()
    {
        var a = Path.Combine(_root, "A.nsp");
        var b = Path.Combine(_root, "B.nsp");
        File.WriteAllText(a, "a");
        File.WriteAllText(b, "b");
        var scanner = new LibraryScanner(_db);

        (await scanner.ScanAsync(_manager, "Switch", _root, ct: TestContext.Current.CancellationToken)).Should().HaveCount(2);
        _parser.Calls.Should().Be(2);

        // Unchanged: served from cache.
        (await scanner.ScanAsync(_manager, "Switch", _root, ct: TestContext.Current.CancellationToken)).Should().HaveCount(2);
        _parser.Calls.Should().Be(2);

        // Modified file is re-parsed, deleted file is dropped.
        File.WriteAllText(a, "changed");
        File.Delete(b);
        var roms = await scanner.ScanAsync(_manager, "Switch", _root, ct: TestContext.Current.CancellationToken);
        _parser.Calls.Should().Be(3);
        roms.Should().ContainSingle().Which.TitleID.Should().Be("A");
        _db.LibraryFiles.Count().Should().Be(1);

        var cached = await scanner.LoadCachedAsync("Switch", TestContext.Current.CancellationToken);
        cached.Should().ContainSingle().Which.Titles![0].Value.Should().Be("Title 3");
    }

    [Fact]
    public async Task ScanAsync_ReportsFilesThatProduceNoRoms()
    {
        var manager = new ConsoleManager<SwitchConsole>(new RomParserExecutor<SwitchConsole>([new EmptyParser()]));
        File.WriteAllText(Path.Combine(_root, "junk.nsp"), "x");
        var scanner = new LibraryScanner(_db);

        (await scanner.ScanAsync(manager, "Switch", _root, ct: TestContext.Current.CancellationToken)).Should().BeEmpty();

        var junk = scanner.Unrecognized.Should().ContainSingle().Which;
        junk.Path.Should().EndWith("junk.nsp");
        junk.Reason.Should().Be("No parser recognised the file");
    }

    private sealed class FailingParser : IRomParser<SwitchConsole>
    {
        public int Calls;
        public List<string> Extensions { get; set; } = ["nsp"];
        public Task<List<Rom>> ProcessFile(string path)
        {
            Calls++;
            throw new InvalidDataException("Missing NCA title key");
        }
    }

    [Fact]
    public async Task ScanAsync_KeepsTheLoggedReasonForUnrecognisedFiles()
    {
        var parser = new FailingParser();
        var manager = new ConsoleManager<SwitchConsole>(new RomParserExecutor<SwitchConsole>([parser]));
        File.WriteAllText(Path.Combine(_root, "locked.nsp"), "x");
        var scanner = new LibraryScanner(_db);
        var ct = TestContext.Current.CancellationToken;

        await scanner.ScanAsync(manager, "Switch", _root, ct: ct);
        scanner.Unrecognized.Should().ContainSingle().Which.Reason.Should().Contain("Missing NCA title key");

        // The reason survives a rescan that serves the file from the cache.
        await scanner.ScanAsync(manager, "Switch", _root, ct: ct);
        parser.Calls.Should().Be(1);
        scanner.Unrecognized.Should().ContainSingle().Which.Reason.Should().Contain("Missing NCA title key");
    }

    private sealed class EmptyParser : IRomParser<SwitchConsole>
    {
        public List<string> Extensions { get; set; } = ["nsp"];
        public Task<List<Rom>> ProcessFile(string path) => Task.FromResult<List<Rom>>([]);
    }

    private sealed class FingerprintProvider : ITitleInfoProvider<SwitchConsole>, IHasDataFingerprint
    {
        public string Source { get; set; } = "test";
        public string DataFingerprint { get; set; } = "";
        public Task LoadTitleDatabaseAsync() => Task.CompletedTask;
        public Task<Rom> GetTitleInfo(Rom rom) => Task.FromResult(rom);
    }

    [Fact]
    public async Task ScanAsync_ReparsesCachedFilesWhenProviderDataChanges()
    {
        File.WriteAllText(Path.Combine(_root, "A.nsp"), "a");
        var provider = new FingerprintProvider();
        var manager = new ConsoleManager<SwitchConsole>(new RomParserExecutor<SwitchConsole>([_parser]),
            new TitleInfoProviderManager<SwitchConsole>([provider]));
        var scanner = new LibraryScanner(_db);
        await scanner.ScanAsync(manager, "Switch", _root, ct: TestContext.Current.CancellationToken);

        provider.DataFingerprint = "dat-v1"; // e.g. a DAT was added
        await scanner.ScanAsync(manager, "Switch", _root, ct: TestContext.Current.CancellationToken);
        _parser.Calls.Should().Be(2);

        await scanner.ScanAsync(manager, "Switch", _root, ct: TestContext.Current.CancellationToken);
        _parser.Calls.Should().Be(2);
    }

    [Fact]
    public async Task ScanAsync_FullRescan_ReparsesEverything()
    {
        File.WriteAllText(Path.Combine(_root, "A.nsp"), "a");
        var scanner = new LibraryScanner(_db);
        await scanner.ScanAsync(_manager, "Switch", _root, ct: TestContext.Current.CancellationToken);

        await scanner.ScanAsync(_manager, "Switch", _root, fullRescan: true, ct: TestContext.Current.CancellationToken);

        _parser.Calls.Should().Be(2);
    }

    [Fact]
    public void RomSnapshot_RoundTripsRuntimeTypeAndNonPublicSetters()
    {
        var game = new PSPGame { TitleID = "ULES00590", Regions = [Region.Europe] };
        typeof(PSPGame).GetProperty(nameof(PSPGame.Category))!.SetValue(game, "UG"); // internal setters
        typeof(PSPGame).GetProperty(nameof(PSPGame.DiskTotal))!.SetValue(game, 2);
        game.AddTitleName("Aces of War");
        var update = new Update { TitleID = "ULES00590", RelatedGame = new Game() };
        update.RelatedGame.Updates = [update]; // cycle

        var roms = RomSnapshot.Deserialize(RomSnapshot.Serialize([game, update]));

        var psp = roms[0].Should().BeOfType<PSPGame>().Subject;
        psp.Category.Should().Be("UG");
        psp.DiskTotal.Should().Be(2);
        psp.Regions.Should().Equal(Region.Europe);
        psp.Titles![0].Value.Should().Be("Aces of War");
        roms[1].Should().BeOfType<Update>();
    }
}
