using AwesomeAssertions;
using RomManagerShared.Configuration;
using RomManagerShared.Switch.Configuration;
using RomManagerShared.ThreeDS.Configuration;
using RomManagerShared.Wii.Configuration;

namespace RomManagerTests;

// RomManagerConfiguration is process-wide: run with the other tests that swap global state, then restore it.
[Collection("ConsoleModules")]
public class ConfigurationTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("rm-config").FullName;

    public void Dispose()
    {
        RomManagerConfiguration.Load("config.json");
        Directory.Delete(_dir, recursive: true);
    }

    private string Write(string json)
    {
        var path = Path.Combine(_dir, "config.json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void MissingKeys_UseDefaults_AndSetKeysWin()
    {
        RomManagerConfiguration.Load(Write($$"""
            {
              "GlobalPaths": { "BaseDirectory": "{{_dir.Replace("\\", "\\\\")}}", "Cache": "MyCache" },
              "Consoles": { "Wii": { "GameTDB": { "Url": "https://example.org/wiitdb.xml" } } }
            }
            """));

        RomManagerConfiguration.BaseFolder.Should().Be(_dir);
        RomManagerConfiguration.GetCachePath().Should().Be(Path.Combine(_dir, "MyCache"));
        RomManagerConfiguration.GetDatsPath().Should().Be(Path.Combine(_dir, "Data/DAT"));
        RomManagerConfiguration.Options.Dats.HashDiscImages.Should().BeFalse();

        WiiConfiguration.GetGameTDBUrl().Should().Be("https://example.org/wiitdb.xml");
        WiiConfiguration.GetGameTDBPath().Should().Be(Path.Combine(_dir, "Data/Wii/wiitdb.xml"));
        // Keys the shipped config.json never had used to read as null; now they have defaults.
        ThreeDSConfiguration.GetTitleDBRegionFilenames().Should().HaveCount(5);
        ThreeDSConfiguration.GetTitleDBBaseUrl().Should().StartWith("https://");
        ThreeDSConfiguration.GetTitleDBPath().Should().EndWith(Path.DirectorySeparatorChar.ToString());
        SwitchConfiguration.GetTitleDBPath().Should().Be(Path.Combine(_dir, "Data/Switch/titledb"));
        SwitchConfiguration.GetTitleDBRegionFiles().Should().BeNull();
    }

    [Fact]
    public void ShippedConfig_BindsConsoleSections()
    {
        RomManagerConfiguration.Load("config.json");

        // A list in the file replaces the default (it is not appended to one).
        SwitchConfiguration.GetTitleDBRegionFiles().Should().Equal("US.en.json", "EU.en.json", "JP.ja.json", "AU.en.json");
        SwitchConfiguration.GetVersionsPath().Should().EndWith("versions.json");
    }

    [Fact]
    public void ChangedFile_IsPickedUpOnReload()
    {
        var path = Write("""{ "Consoles": { "Switch": { "RomPath": "A:/Switch" } } }""");
        RomManagerConfiguration.Load(path);
        RomManagerConfiguration.GetConsoleRomPath("Switch").Should().Be("A:/Switch");
        RomManagerConfiguration.Options.Dats.HashDiscImages.Should().BeFalse();

        File.WriteAllText(path, """{ "Dats": { "HashDiscImages": true }, "Consoles": { "Switch": { "RomPath": "B:/Switch" } } }""");
        RomManagerConfiguration.Configuration.Reload();

        RomManagerConfiguration.GetConsoleRomPath("Switch").Should().Be("B:/Switch");
        RomManagerConfiguration.Options.Dats.HashDiscImages.Should().BeTrue();
    }

    [Fact]
    public void UnderBase_KeepsAbsolutePaths()
    {
        var absolute = Path.Combine(_dir, "tools", "chdman.exe");
        RomManagerConfiguration.UnderBase(absolute).Should().Be(absolute);
        RomManagerConfiguration.UnderBase(null, "Tools/x").Should().Be(Path.Combine(RomManagerConfiguration.BaseFolder, "Tools/x"));
    }
}
