using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using RomManagerShared.Base;
using RomManagerShared.Utils;

namespace RomManagerTests;

public class OperationJournalTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private string JournalPath => Path.Combine(_root, "journal.json");

    public OperationJournalTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private static async Task WaitFinished(QueuedOperation op)
    {
        for (int i = 0; i < 200 && !op.IsFinished; i++) await Task.Delay(20, TestContext.Current.CancellationToken);
        op.State.Should().Be(OperationState.Completed);
    }

    [Fact]
    public void Journal_PersistsAndRemovesEntries()
    {
        var journal = new OperationJournal(JournalPath);
        var entry = journal.Add("Move 1 ROM(s)", "Move", [new PathMove("b", "a", false)]);

        var reloaded = new OperationJournal(JournalPath).Entries.Should().ContainSingle().Which;
        reloaded.Id.Should().Be(entry.Id);
        reloaded.Undo.Should().ContainSingle().Which.To.Should().Be("a");

        journal.Remove(entry.Id);
        new OperationJournal(JournalPath).Entries.Should().BeEmpty();
    }

    [Fact]
    public void Journal_KeepsOnlyTheNewestEntries()
    {
        var journal = new OperationJournal(JournalPath, maxEntries: 2);
        journal.Add("1", "Move", []);
        journal.Add("2", "Move", []);
        journal.Add("3", "Move", []);

        new OperationJournal(JournalPath).Entries.Select(e => e.Name).Should().Equal("2", "3");
    }

    [Fact]
    public async Task MoveCanBeUndoneInALaterSession()
    {
        var file = Path.Combine(_root, "src", "game.nes");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "x");
        var target = Path.Combine(_root, "dst");
        var services = new ServiceCollection().BuildServiceProvider();

        var first = new RomOperationService(new OperationQueue(), services, new OperationJournal(JournalPath));
        await WaitFinished(first.EnqueueMove([new Game { Path = file }], target));
        File.Exists(file).Should().BeFalse();

        // "Restart": a new queue and service read the journal back.
        var queue = new OperationQueue();
        var second = new RomOperationService(queue, services, new OperationJournal(JournalPath));
        second.RestoreJournal();
        var restored = queue.Operations.Should().ContainSingle().Which;
        restored.CanUndo.Should().BeTrue();

        await queue.UndoAsync(restored);

        restored.State.Should().Be(OperationState.Undone);
        File.Exists(file).Should().BeTrue();
        new OperationJournal(JournalPath).Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearingFinishedOperationsForgetsTheirJournalEntries()
    {
        var file = Path.Combine(_root, "game.nes");
        File.WriteAllText(file, "x");
        var queue = new OperationQueue();
        var service = new RomOperationService(queue, new ServiceCollection().BuildServiceProvider(), new OperationJournal(JournalPath));

        await WaitFinished(service.EnqueueRename(new Game { Path = file, Titles = [new() { Value = "Renamed" }] }, "{TitleName}"));
        new OperationJournal(JournalPath).Entries.Should().ContainSingle();

        queue.ClearFinished();
        new OperationJournal(JournalPath).Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task SingleRenameUndoRestoresFolderFormatRoms()
    {
        var game = Path.Combine(_root, "Old Name");
        Directory.CreateDirectory(Path.Combine(game, "code"));
        File.WriteAllText(Path.Combine(game, "code", "app.xml"), "<app/>");
        var rom = new RomManagerShared.WiiU.WiiUGame
        {
            Path = Path.Combine(game, "code", "app.xml"), IsFolderFormat = true, Titles = [new() { Value = "New Name" }],
        };
        var queue = new OperationQueue();
        var service = new RomOperationService(queue, new ServiceCollection().BuildServiceProvider());

        var op = service.EnqueueRename(rom, "{TitleName}");
        await WaitFinished(op);
        Directory.Exists(game).Should().BeFalse();

        await queue.UndoAsync(op);

        op.State.Should().Be(OperationState.Undone);
        rom.Path.Should().Be(Path.Combine(game, "code", "app.xml"));
        File.Exists(rom.Path).Should().BeTrue();
    }

    [Fact]
    public async Task BulkRename_ThatPartlyFails_CompletesWithAWarning()
    {
        var good = new Game { Path = Path.Combine(_root, "a.nsp"), TitleID = "01", Titles = [new() { Value = "Named" }] };
        var untitled = new Game { Path = Path.Combine(_root, "b.nsp"), TitleID = "02" };
        File.WriteAllText(good.Path, "a");
        File.WriteAllText(untitled.Path, "b");
        var service = new RomOperationService(new OperationQueue(), new ServiceCollection().BuildServiceProvider());

        var op = service.EnqueueBulkRename([good, untitled], "{TitleName}");
        await WaitFinished(op);

        op.Warning.Should().StartWith("1 of 2 failed").And.Contain(FileRenamer.EmptyNameError);
        File.Exists(Path.Combine(_root, "Named.nsp")).Should().BeTrue();
        File.Exists(untitled.Path).Should().BeTrue();
    }
}
