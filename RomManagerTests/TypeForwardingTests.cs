using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Base.Library;

namespace RomManagerTests;

public class TypeForwardingTests
{
    // Library snapshots written before the rom model moved name its types "…, RomManagerShared".
    [Fact]
    public void OldTypeNames_StillResolve()
    {
        Type.GetType("RomManagerShared.Base.Game, RomManagerShared").Should().Be(typeof(Game));
        typeof(Game).Assembly.GetName().Name.Should().Be("RomManager.Abstractions");

        const string old = """[{"Type":"RomManagerShared.Base.Game, RomManagerShared","Data":{"TitleID":"A","Path":"/a.nes"}}]""";
        RomSnapshot.Deserialize(old).Should().ContainSingle().Which.Should().BeOfType<Game>().Which.TitleID.Should().Be("A");
    }

    [Fact]
    public void ThePluginContract_LivesInTheAbstractions() =>
        typeof(IPlugin).Assembly.Should().BeSameAs(typeof(Rom).Assembly);
}
