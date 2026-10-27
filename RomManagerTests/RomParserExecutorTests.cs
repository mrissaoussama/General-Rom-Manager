using AwesomeAssertions;
using RomManagerShared;
using RomManagerShared.Base;
using RomManagerShared.Interfaces;

namespace RomManagerTests;

public class RomParserExecutorTests
{
    private sealed class FakeParser(List<string> extensions, Func<string, List<Rom>> parse) : IRomParser<SwitchConsole>
    {
        public List<string> Extensions { get; set; } = extensions;
        public Task<List<Rom>> ProcessFile(string path) => Task.FromResult(parse(path));
    }

    private sealed class SniffingParser(byte magic, Func<string, List<Rom>> parse) : IRomParser<SwitchConsole>, IContentSniffer
    {
        public List<string> Extensions { get; set; } = ["iso"];
        public int Calls { get; private set; }
        public Task<List<Rom>> ProcessFile(string path) { Calls++; return Task.FromResult(parse(path)); }
        public bool CanParse(string path, ReadOnlySpan<byte> header) => header.Length > 0 && header[0] == magic;
    }

    [Fact]
    public async Task ExecuteParsers_DropsEmptyRoms()
    {
        var parser = new FakeParser(["iso"], _ => [new Game(), new Game { TitleID = "ULES00590" }]);
        var executor = new RomParserExecutor<SwitchConsole>([parser]);

        var roms = await executor.ExecuteParsers("game.iso", TestContext.Current.CancellationToken);

        roms.Should().ContainSingle().Which.TitleID.Should().Be("ULES00590");
    }

    [Fact]
    public async Task ExecuteParsers_KeepsTitleOnlyRoms()
    {
        var rom = new Game();
        rom.AddTitleName("POKEMON RED");
        var executor = new RomParserExecutor<SwitchConsole>([new FakeParser(["gb"], _ => [rom])]);

        var roms = await executor.ExecuteParsers("red.gb", TestContext.Current.CancellationToken);

        roms.Should().ContainSingle();
    }

    [Fact]
    public async Task ExecuteParsers_MergesByTitleId()
    {
        var a = new FakeParser(["nsp"], _ => [new Game { TitleID = "0100A", Version = "0" }]);
        var b = new FakeParser(["nsp"], _ => [new Game { TitleID = "0100A", Publisher = "Nintendo" }]);
        var executor = new RomParserExecutor<SwitchConsole>([a, b]);

        var roms = await executor.ExecuteParsers("x.nsp", TestContext.Current.CancellationToken);

        roms.Should().ContainSingle();
        roms[0].Version.Should().Be("0");
        roms[0].Publisher.Should().Be("Nintendo");
    }

    [Fact]
    public async Task ExecuteParsers_SwallowsParserExceptions()
    {
        var bad = new FakeParser(["nsp"], _ => throw new InvalidDataException());
        var good = new FakeParser(["nsp"], _ => [new Game { TitleID = "0100A" }]);
        var executor = new RomParserExecutor<SwitchConsole>([bad, good]);

        var roms = await executor.ExecuteParsers("x.nsp", TestContext.Current.CancellationToken);

        roms.Should().ContainSingle();
    }

    [Fact]
    public void Constructor_OrdersParsersByExtensionCount()
    {
        var one = new FakeParser(["nsp"], _ => []);
        var two = new FakeParser(["nsp", "nsz"], _ => []);
        var executor = new RomParserExecutor<SwitchConsole>([one, two]);

        executor.Parsers[0].Should().BeSameAs(two);
    }

    [Fact]
    public async Task ParseAsync_SkipsParsersWhoseMagicDoesNotMatch()
    {
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".iso");
        File.WriteAllBytes(file, [0xB0, 1, 2, 3]);
        try
        {
            var wrong = new SniffingParser(0xA0, _ => [new Game { TitleID = "WRONG" }]);
            var right = new SniffingParser(0xB0, _ => [new Game { TitleID = "RIGHT" }]);
            var executor = new RomParserExecutor<SwitchConsole>([wrong, right]);

            var result = await executor.ParseAsync(file, TestContext.Current.CancellationToken);

            result.Roms.Should().ContainSingle().Which.TitleID.Should().Be("RIGHT");
            wrong.Calls.Should().Be(0);
            result.UnidentifiedReason.Should().BeNull();
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task ParseAsync_ExplainsAFileNoParserAccepts()
    {
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".iso");
        File.WriteAllBytes(file, [0xFF]);
        try
        {
            var executor = new RomParserExecutor<SwitchConsole>([new SniffingParser(0xA0, _ => [])]);

            var result = await executor.ParseAsync(file, TestContext.Current.CancellationToken);

            result.Roms.Should().BeEmpty();
            result.UnidentifiedReason.Should().Contain("not a Switch file");
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task ExecuteParsers_StopsWhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        var first = new FakeParser(["nsp"], _ => { cts.Cancel(); return [new Game { TitleID = "A" }]; });
        var second = new FakeParser(["nsp"], _ => throw new InvalidOperationException("should not run"));
        var executor = new RomParserExecutor<SwitchConsole>([first, second]);

        await executor.Invoking(e => e.ExecuteParsers("x.nsp", cts.Token)).Should().ThrowAsync<OperationCanceledException>();
    }
}
