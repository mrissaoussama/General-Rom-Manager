using System.Text.Json;
using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Utils;

namespace RomManagerTests;

public class LibraryExporterTests
{
    private static ExportRow Row(string title, string path = "game.nsp") =>
        ExportRow.From("Switch", Game(title, path));

    private static Game Game(string title, string path)
    {
        var game = new Game { TitleID = "0100000000010000", Version = "65536", Path = path, Size = 1234, Regions = [Region.USA, Region.Europe] };
        game.AddTitleName(title);
        game.Hashes = [new RomHash { Type = HashTypeEnum.SHA1, Value = "abc", IsVerified = true }];
        return game;
    }

    [Fact]
    public void From_MapsRomFields()
    {
        var row = Row("Zelda", "dir/zelda.nsp");

        row.Should().Be(new ExportRow("Switch", "Game", "0100000000010000", "Zelda", "65536", "USA; Europe", "", "NSP", 1234,
            "dir/zelda.nsp", "abc", true));
        ExportRow.From("Switch", new Update { TitleID = "x" }).Type.Should().Be("Update");
    }

    [Fact]
    public void WriteCsv_QuotesSeparatorsAndNeutralisesFormulas()
    {
        var writer = new StringWriter();

        LibraryExporter.WriteCsv([Row("Mario, \"Deluxe\""), Row("=HYPERLINK(\"x\")")], writer);

        var lines = writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines[0].Should().Be("Console,Type,TitleId,Title,Version,Regions,Languages,Format,Size,Path,Sha1,Verified");
        lines[1].Should().Contain(",\"Mario, \"\"Deluxe\"\"\",");
        lines[2].Should().Contain(",\"'=HYPERLINK(\"\"x\"\")\",");
    }

    [Fact]
    public void WriteJson_RoundTrips()
    {
        var stream = new MemoryStream();

        LibraryExporter.WriteJson([Row("Zelda")], stream);

        var rows = JsonSerializer.Deserialize<List<ExportRow>>(stream.ToArray());
        rows.Should().ContainSingle().Which.Title.Should().Be("Zelda");
    }
}
