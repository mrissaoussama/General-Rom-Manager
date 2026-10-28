using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using RomManagerShared.Base;
using RomManagerShared.Base.Library;

namespace RomManagerTests;

public sealed class LibraryImportTests : IDisposable
{
    private sealed class Ds;
    private sealed class Psx;
    private sealed class Ps2;

    private static readonly ConsoleDescriptor DsConsole = new(typeof(Ds), "DS", "Nintendo DS");
    private static readonly ConsoleDescriptor PsxConsole = new(typeof(Psx), "PS1", "PlayStation");
    private static readonly ConsoleDescriptor Ps2Console = new(typeof(Ps2), "PS2", "PlayStation 2");

    private readonly string _dir = Directory.CreateTempSubdirectory("import").FullName;
    private string Source => Path.Combine(_dir, "incoming");
    private string Library => Path.Combine(_dir, "library");

    public LibraryImportTests() => Directory.CreateDirectory(Source);

    public void Dispose() => Directory.Delete(_dir, true);

    private static LibraryImport Import() => new([DsConsole, PsxConsole, Ps2Console], new Dictionary<Type, IReadOnlyCollection<string>>
    {
        [typeof(Ds)] = ["nds"],
        [typeof(Psx)] = ["cue", "iso"],
        [typeof(Ps2)] = ["iso"],
    });

    private string File(string name, string content = "x")
    {
        var path = Path.Combine(Source, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Plan_SendsEachFileToItsConsoleFolder()
    {
        var nds = File("Mario.nds");

        var item = Import().Plan([nds], Library, ImportMode.Move).Should().ContainSingle().Subject;

        item.Console.Should().Be(DsConsole);
        item.Destination.Should().Be(Path.Combine(Library, "Nintendo DS", "Mario.nds"));
        item.WillChange.Should().BeTrue();
    }

    [Fact]
    public void Plan_KeepsCueTracksWithTheirSheet()
    {
        var cue = File("Game.cue");
        var track1 = File("Game (Track 1).bin");
        var track2 = File("Game (Track 2).bin");
        var other = File("Game 2.cue");
        var otherTrack = File("Game 2 (Track 1).bin");

        var plan = Import().Plan(LibraryImport.Entries(Source), Library, ImportMode.Move);

        plan.Should().HaveCount(2);
        plan.Single(i => i.Source == cue).Companions.Should().BeEquivalentTo([track1, track2]);
        plan.Single(i => i.Source == other).Companions.Should().Equal(otherTrack);
    }

    [Fact]
    public void Plan_SkipsWhatItCannotPlace()
    {
        var text = File("readme.txt");
        var iso = File("disc.iso"); // PS1 or PS2: the header check could not settle it
        File(Path.Combine("..", "library", "Nintendo DS", "Mario.nds"));
        var nds = File("Mario.nds");

        var plan = Import().Plan([text, iso, nds], Library, ImportMode.Move);

        plan.Single(i => i.Source == text).Skip.Should().Be("Not a ROM format");
        plan.Single(i => i.Source == iso).Skip.Should().Contain("PlayStation");
        plan.Single(i => i.Source == nds).Skip.Should().Contain("already there");
        plan.Should().OnlyContain(i => !i.WillChange);
    }

    [Fact]
    public void Plan_ClassifiesAGameFolderByItsFiles()
    {
        File(Path.Combine("Game", "Game.cue"));
        File(Path.Combine("Game", "Game.bin"));

        var item = Import().Plan(LibraryImport.Entries(Source), Library, ImportMode.Move).Should().ContainSingle().Subject;

        item.IsDirectory.Should().BeTrue();
        item.Console.Should().Be(PsxConsole);
        item.Destination.Should().Be(Path.Combine(Library, "PlayStation", "Game"));
    }

    [Fact]
    public void Plan_KeepInPlace_OnlyClassifies()
    {
        var nds = File("Mario.nds");

        var item = Import().Plan([nds], Library, ImportMode.KeepInPlace).Single();

        item.Console.Should().Be(DsConsole);
        item.Destination.Should().BeNull();
        item.WillChange.Should().BeTrue();
    }

    [Fact]
    public void Execute_Move_MovesSheetWithTracksAndUndoPutsThemBack()
    {
        var cue = File("Game.cue");
        var track = File("Game (Track 1).bin");
        var plan = Import().Plan([cue, track], Library, ImportMode.Move);

        var result = LibraryImport.Execute(plan, ImportMode.Move, ct: TestContext.Current.CancellationToken);

        result.Done.Should().Be(1);
        result.Failed.Should().BeEmpty();
        System.IO.File.Exists(Path.Combine(Library, "PlayStation", "Game.cue")).Should().BeTrue();
        System.IO.File.Exists(Path.Combine(Library, "PlayStation", "Game (Track 1).bin")).Should().BeTrue();
        System.IO.File.Exists(cue).Should().BeFalse();

        RomOperationService.ApplyRestorePlan(result.Undo, TestContext.Current.CancellationToken);

        System.IO.File.Exists(cue).Should().BeTrue();
        System.IO.File.Exists(track).Should().BeTrue();
    }

    [Fact]
    public void Execute_Copy_KeepsOriginalsAndUndoDeletesTheCopies()
    {
        var nds = File("Mario.nds");
        var plan = Import().Plan([nds], Library, ImportMode.Copy);

        var result = LibraryImport.Execute(plan, ImportMode.Copy, ct: TestContext.Current.CancellationToken);

        var copy = Path.Combine(Library, "Nintendo DS", "Mario.nds");
        System.IO.File.Exists(copy).Should().BeTrue();
        System.IO.File.Exists(nds).Should().BeTrue();
        result.Undo.Should().BeEmpty();

        LibraryImport.DeleteCreated(result.Created);

        System.IO.File.Exists(copy).Should().BeFalse();
    }

    [Fact]
    public async Task EnqueueImport_RunsInTheQueueAndUndoes()
    {
        var nds = File("Mario.nds");
        var plan = Import().Plan([nds], Library, ImportMode.Move);
        var queue = new OperationQueue();
        var service = new RomOperationService(queue, new ServiceCollection().BuildServiceProvider());

        var op = service.EnqueueImport(plan, ImportMode.Move);
        for (int i = 0; i < 200 && !op.IsFinished; i++) await Task.Delay(20, TestContext.Current.CancellationToken);

        op.State.Should().Be(OperationState.Completed);
        System.IO.File.Exists(Path.Combine(Library, "Nintendo DS", "Mario.nds")).Should().BeTrue();

        await queue.UndoAsync(op);
        System.IO.File.Exists(nds).Should().BeTrue();
    }

    [Fact]
    public void ConsoleFolderName_IsTheDisplayNameMadeSafe() =>
        LibraryImport.ConsoleFolderName(new ConsoleDescriptor(typeof(Ds), "X", "Sega Mega Drive / Genesis"))
            .Should().Be("Sega Mega Drive _ Genesis");
}
