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

    [Fact]
    public async Task ExecuteParsers_DropsEmptyRoms()
    {
        var parser = new FakeParser(["iso"], _ => [new Game(), new Game { TitleID = "ULES00590" }]);
        var executor = new RomParserExecutor<SwitchConsole>([parser]);

        var roms = await executor.ExecuteParsers("game.iso");

        roms.Should().ContainSingle().Which.TitleID.Should().Be("ULES00590");
    }

    [Fact]
    public async Task ExecuteParsers_KeepsTitleOnlyRoms()
    {
        var rom = new Game();
        rom.AddTitleName("POKEMON RED");
        var executor = new RomParserExecutor<SwitchConsole>([new FakeParser(["gb"], _ => [rom])]);

        var roms = await executor.ExecuteParsers("red.gb");

        roms.Should().ContainSingle();
    }

    [Fact]
    public async Task ExecuteParsers_MergesByTitleId()
    {
        var a = new FakeParser(["nsp"], _ => [new Game { TitleID = "0100A", Version = "0" }]);
        var b = new FakeParser(["nsp"], _ => [new Game { TitleID = "0100A", Publisher = "Nintendo" }]);
        var executor = new RomParserExecutor<SwitchConsole>([a, b]);

        var roms = await executor.ExecuteParsers("x.nsp");

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

        var roms = await executor.ExecuteParsers("x.nsp");

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
}
