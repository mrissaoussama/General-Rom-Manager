using System.Text.Json;
using System.Xml.Linq;
using AwesomeAssertions;
using RomManager.Plugins.GameTdbCovers;
using RomManager.Plugins.Igdb;
using RomManager.Plugins.Playlists;
using RomManager.Plugins.WebSearch;
using RomManagerShared.Base;

namespace RomManagerTests;

public class BuiltInPluginTests
{
    private static Game Game(string title, string path, string? crc = null) => new()
    {
        TitleID = title, Titles = [new RomTitle(title)], Path = path,
        Hashes = crc is null ? null : [new RomHash { Type = HashTypeEnum.CRC32, Value = crc }],
    };

    // ── Web search ────────────────────────────────────────────────────────

    [Fact]
    public void WebSearch_EncodesTheQuery_AndRefusesNonWebTemplates()
    {
        WebSearchPlugin.BuildUrl("https://duckduckgo.com/?q={query}", "Zelda & Link Switch")!.AbsoluteUri
            .Should().Be("https://duckduckgo.com/?q=Zelda%20%26%20Link%20Switch");
        WebSearchPlugin.BuildUrl("file:///c:/{query}", "x").Should().BeNull();
        WebSearchPlugin.BuildUrl("https://example.org/", "x").Should().BeNull(); // no {query}
    }

    [Fact]
    public void WebSearch_QueryIsTitleAndConsole()
    {
        WebSearchPlugin.Query(Game("Metroid", "m.nes"), "Nintendo Entertainment System").Should().Be("Metroid Nintendo Entertainment System");
        WebSearchPlugin.Query(new Game { Path = "x.nes" }, "NES").Should().BeNull();
    }

    // ── Playlists ─────────────────────────────────────────────────────────

    [Fact]
    public void Playlists_SkipUpdatesAndDlc_AndListEachFileOnce()
    {
        var roms = new Rom[]
        {
            Game("B game", "/roms/b.nsp"),
            Game("A game", "/roms/a.nsp"),
            new Update { TitleID = "u", Path = "/roms/a.nsp" },
            new DLC { TitleID = "d", Path = "/roms/d.nsp" },
            Game("A game again", "/roms/a.nsp"),
        };

        PlaylistsPlugin.Playable(roms).Select(r => r.Titles![0].Value).Should().Equal("A game", "B game");
    }

    [Fact]
    public void RetroArchPlaylist_HasTheFieldsRetroArchReads()
    {
        var json = JsonDocument.Parse(PlaylistsPlugin.RetroArchPlaylist([Game("Metroid", "/roms/metroid.nes", "a1b2c3d4"), Game("Kirby", "/roms/k.nes")], "Nintendo - NES.lpl"));

        json.RootElement.GetProperty("version").GetString().Should().Be("1.5");
        var items = json.RootElement.GetProperty("items").EnumerateArray().ToList();
        items[0].GetProperty("path").GetString().Should().Be("/roms/metroid.nes");
        items[0].GetProperty("label").GetString().Should().Be("Metroid");
        items[0].GetProperty("crc32").GetString().Should().Be("A1B2C3D4|crc");
        items[0].GetProperty("db_name").GetString().Should().Be("Nintendo - NES.lpl");
        items[1].GetProperty("crc32").GetString().Should().Be("DETECT");
        items[1].GetProperty("core_path").GetString().Should().Be("DETECT");
    }

    [Fact]
    public void Gamelist_UsesPathsRelativeToItsFolder_AndOptionalFields()
    {
        var folder = Path.Combine(Path.GetTempPath(), "es-roms");
        var inside = Game("Metroid", Path.Combine(folder, "sub", "metroid.nes"));
        inside.ReleaseDate = new DateOnly(1986, 8, 6);
        inside.Developer = "Nintendo R&D1";
        var outside = Game("Kirby", Path.Combine(Path.GetTempPath(), "elsewhere", "kirby.nes"));

        var xml = XDocument.Parse(PlaylistsPlugin.EmulationStationGamelist([inside, outside], folder));

        var games = xml.Root!.Elements("game").ToList();
        games[0].Element("path")!.Value.Should().Be("./sub/metroid.nes");
        games[0].Element("releasedate")!.Value.Should().Be("19860806T000000");
        games[0].Element("developer")!.Value.Should().Be("Nintendo R&D1");
        games[1].Element("path")!.Value.Should().Be(outside.Path);
        games[1].Element("releasedate").Should().BeNull();
    }

    // ── GameTDB ───────────────────────────────────────────────────────────

    [Fact]
    public void GameTdb_TriesTheGamesRegionFirst()
    {
        var game = new Game { TitleID = "0004000000030700", ProductCode = "CTR-P-AMKJ" };

        GameTdbCoversPlugin.CandidateUrls(game, "3ds", "jpg").Select(u => u.AbsoluteUri).Should().Equal(
            "https://art.gametdb.com/3ds/cover/JA/AMKJ.jpg",
            "https://art.gametdb.com/3ds/cover/US/AMKJ.jpg",
            "https://art.gametdb.com/3ds/cover/EN/AMKJ.jpg");
    }

    [Fact]
    public void GameTdb_UsesWiiGameIdsAndWiiUMakerCode()
    {
        GameTdbCoversPlugin.CandidateUrls(new Game { TitleID = "RMGE01" }, "wii", "png").First().AbsoluteUri
            .Should().Be("https://art.gametdb.com/wii/cover/US/RMGE01.png");
        GameTdbCoversPlugin.CandidateUrls(new Game { TitleID = "0005000010101D00", ProductCode = "WUP-P-ARPE" }, "wiiu", "jpg").First().AbsoluteUri
            .Should().Be("https://art.gametdb.com/wiiu/cover/US/ARPE01.jpg");
        GameTdbCoversPlugin.RegionOf("BLES00001", "ps3").Should().Be("EN");
    }

    // ── IGDB ──────────────────────────────────────────────────────────────

    [Fact]
    public void Igdb_QueryEscapesTheTitleAndFiltersByPlatform()
    {
        IgdbPlugin.Query("Say \"Hi\"", [37]).Should().StartWith("search \"Say \\\"Hi\\\"\";")
            .And.Contain("where platforms = (37);").And.EndWith("limit 1;");
        IgdbPlugin.Query("Zelda", null).Should().NotContain("where");
    }

    [Fact]
    public void Igdb_ReadsTheReleaseDateAndCompanies()
    {
        var game = System.Text.Json.JsonSerializer.Deserialize<IgdbPlugin.IgdbGame>("""
            {"name":"Mario Kart 7","first_release_date":1322697600,
             "involved_companies":[{"developer":true,"publisher":false,"company":{"name":"Retro Studios"}},
                                   {"developer":false,"publisher":true,"company":{"name":"Nintendo"}}]}
            """)!;

        game.ReleaseDate.Should().Be(new DateOnly(2011, 12, 1));
        game.Company(developer: true).Should().Be("Retro Studios");
        game.Company(developer: false).Should().Be("Nintendo");
    }
}
