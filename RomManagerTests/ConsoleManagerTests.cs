using AwesomeAssertions;
using RomManagerShared;
using RomManagerShared.Base;
using RomManagerShared.Interfaces;

using RomManagerShared.Switch;

namespace RomManagerTests;

public class ConsoleManagerTests
{
    private sealed class FakeParser : IRomParser<SwitchConsole>
    {
        public List<string> Extensions { get; set; } = ["nsp"];
        public Task<List<Rom>> ProcessFile(string path) => Task.FromResult<List<Rom>>([new Game { TitleID = "0100A" }]);
    }

    // Re-types the rom like SwitchJsonTitleInfoProvider does for updates.
    private sealed class RetypingProvider : TitleInfoProvider<SwitchConsole>
    {
        public override Task<Rom> GetTitleInfo(Rom rom)
        {
            var update = new Update { TitleID = rom.TitleID };
            update.AddTitleName("Enriched");
            return Task.FromResult<Rom>(update);
        }
    }

    private sealed class ThrowingProvider : TitleInfoProvider<SwitchConsole>
    {
        public override Task<Rom> GetTitleInfo(Rom rom) => throw new InvalidOperationException();
    }

    [Fact]
    public async Task ProcessFile_EnrichesRomsWithTitleProviders()
    {
        var manager = new ConsoleManager<SwitchConsole>(
            new RomParserExecutor<SwitchConsole>([new FakeParser()]),
            new TitleInfoProviderManager<SwitchConsole>([new ThrowingProvider(), new RetypingProvider()]));

        await manager.ProcessFile("game.nsp");

        var rom = manager.RomList.Should().ContainSingle().Subject;
        rom.Should().BeOfType<Update>();
        rom.Titles![0].Value.Should().Be("Enriched");
    }
}
