using System.Text;
using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Utils.Dat;

namespace RomManagerTests;

public class OneGameOneRomTests
{
    private static Game Verified(string datName, string? version = null)
    {
        var rom = new Game { Path = $"/roms/{datName}.gb", Version = version };
        rom.AddTitleName(DatIndex.CleanTitle(datName));
        rom.Regions = DatIndex.RegionsFromName(datName);
        rom.Hashes = [new RomHash { Type = HashTypeEnum.SHA1, Value = datName, IsVerified = true, Description = datName }];
        return rom;
    }

    [Fact]
    public void KeepsTheFirstRegionOfThePriorityList()
    {
        var usa = Verified("Tetris (USA)");
        var jp = Verified("Tetris (Japan)");
        var eu = Verified("Tetris (Europe)");

        var group = OneGameOneRom.Select([jp, eu, usa], [Region.Europe, Region.USA]).Should().ContainSingle().Subject;

        group.Game.Should().Be("Tetris");
        group.Keep.Should().BeSameAs(eu);
        group.Others.Should().BeEquivalentTo([usa, jp], o => o.WithoutStrictOrdering().ExcludingMissingMembers());
    }

    [Fact]
    public void PrefersGoodDumpsAndNewerRevisions()
    {
        var beta = Verified("Metroid (USA) (Beta)");
        var rev0 = Verified("Metroid (USA)");
        var rev1 = Verified("Metroid (USA) (Rev 1)");

        OneGameOneRom.Select([beta, rev0, rev1], OneGameOneRom.DefaultPriority)
            .Should().ContainSingle().Which.Keep.Should().BeSameAs(rev1);
    }

    [Fact]
    public void SingleVersionsAndUpdatesAreLeftAlone()
    {
        var update = new Update { Path = "/roms/u.nsp" };
        update.AddTitleName("Tetris");

        OneGameOneRom.Select([Verified("Tetris (USA)"), Verified("Zelda (Japan)"), update], OneGameOneRom.DefaultPriority)
            .Should().BeEmpty();
    }

    [Fact]
    public void ParentCloneDats_GroupRetitledVersions()
    {
        var dat = new DatIndex();
        dat.Add(new MemoryStream(Encoding.UTF8.GetBytes("""
            <datafile>
              <game name="Pocket Monsters Aka (Japan)"><rom name="a.gb" size="1" crc="00000001"/></game>
              <game name="Pokemon - Red Version (USA, Europe)" cloneof="Pocket Monsters Aka (Japan)"><rom name="b.gb" size="1" crc="00000002"/></game>
            </datafile>
            """)));
        var jp = Verified("Pocket Monsters Aka (Japan)");
        var us = Verified("Pokemon - Red Version (USA, Europe)");

        var group = OneGameOneRom.Select([jp, us], OneGameOneRom.DefaultPriority, [dat]).Should().ContainSingle().Subject;

        group.Keep.Should().BeSameAs(us);
        group.Game.Should().Be("Pocket Monsters Aka");
    }

    [Fact]
    public void ParsePriority_AcceptsNoIntroSpellings()
    {
        OneGameOneRom.ParsePriority("Europe, UK, USA").Should().Equal(Region.Europe, Region.UnitedKingdom, Region.USA);
        OneGameOneRom.ParsePriority("").Should().Equal(OneGameOneRom.DefaultPriority);
    }
}
