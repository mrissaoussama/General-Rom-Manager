using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Utils;
using RomManagerShared.Utils.Dat;

namespace RomManagerTests;

public sealed class BatchOperationTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("batch").FullName;
    public void Dispose() => Directory.Delete(_dir, true);

    private string File(string name)
    {
        var path = Path.Combine(_dir, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllText(path, name);
        return path;
    }

    private static T Titled<T>(T rom, string id, string title, string path) where T : Rom
    {
        rom.TitleID = id;
        rom.Path = path;
        rom.AddTitleName(title);
        return rom;
    }

    [Fact]
    public void GroupByGame_MovesTheGameWithItsUpdateAndDlc()
    {
        var game = Titled(new Game(), "0004000000030700", "Mario Kart 7", File("mk7.3ds"));
        var update = Titled(new Update(), "0004000E00030700", "MK7 Update", File("mk7 update.cia"));
        var dlc = Titled(new DLC(), "0004008C00030700", "MK7 DLC", File("sub/mk7 dlc.cia"));
        RomRelations.Link([game, update, dlc]);

        var plan = RomGrouping.Plan([game, update, dlc]);

        var folder = Path.Combine(_dir, "Mario Kart 7");
        plan.Select(p => p.Destination).Should().BeEquivalentTo(
            [Path.Combine(folder, "mk7.3ds"), Path.Combine(folder, "mk7 update.cia"), Path.Combine(folder, "mk7 dlc.cia")]);

        var (moved, failed) = RomGrouping.Execute(plan, [game, update, dlc], TestContext.Current.CancellationToken);

        moved.Should().Be(3);
        failed.Should().BeEmpty();
        update.Path.Should().Be(Path.Combine(folder, "mk7 update.cia"));
        System.IO.File.Exists(Path.Combine(folder, "mk7 dlc.cia")).Should().BeTrue();
        RomGrouping.Plan([game, update, dlc]).Should().BeEmpty("they are in place now");
    }

    [Fact]
    public void GroupByGame_UpdateJoinsAGameAlreadyInItsFolder()
    {
        var game = Titled(new Game(), "ULES00590", "Daxter", File("Daxter/daxter.iso"));
        var update = Titled(new Update(), "ULES00590", "Daxter", File("daxter-patch.pbp"));
        RomRelations.Link([game, update]);

        RomGrouping.Plan([update]).Should().ContainSingle()
            .Which.Destination.Should().Be(Path.Combine(_dir, "Daxter", "daxter-patch.pbp"));
    }

    [Fact]
    public void GroupByGame_FailsAFileWhoseDestinationExists()
    {
        var game = Titled(new Game(), "1", "Zelda", File("zelda.nes"));
        File("Zelda/zelda.nes");

        var (moved, failed) = RomGrouping.Execute(RomGrouping.Plan([game]), [game], TestContext.Current.CancellationToken);

        moved.Should().Be(0);
        failed.Should().ContainSingle();
        game.Path.Should().Be(Path.Combine(_dir, "zelda.nes"));
    }

    [Fact]
    public void DatVerifier_UsesTheFilesOwnHashes()
    {
        var payload = Encoding.UTF8.GetBytes("rom data");
        var sha1 = Convert.ToHexStringLower(SHA1.HashData(payload));
        var crc = Crc32.HashToUInt32(payload).ToString("x8");
        var index = new DatIndex();
        index.Add(new MemoryStream(Encoding.UTF8.GetBytes($"""
            <?xml version="1.0"?>
            <datafile><header><name>T</name></header>
              <game name="Good Game (USA)"><rom name="g.nes" size="{payload.Length}" crc="{crc}" sha1="{sha1}"/></game>
            </datafile>
            """)));
        var path = Path.Combine(_dir, "g.nes");
        System.IO.File.WriteAllBytes(path, payload);
        RomHash Local(HashTypeEnum type, string value) => new() { Type = type, Value = value.ToUpperInvariant(), Description = "Local" };

        var good = new Game { Path = path, Hashes = [Local(HashTypeEnum.SHA1, sha1), Local(HashTypeEnum.CRC32, crc)] };
        var bad = new Game { Path = path, Hashes = [Local(HashTypeEnum.SHA1, new string('0', 40))] };

        DatVerifier.Verify(good, [index]).Should().Match<DatCheck>(c => c.Verdict == DatVerdict.Verified && c.Match!.Game == "Good Game (USA)");
        DatVerifier.Verify(bad, [index]).Verdict.Should().Be(DatVerdict.NotInDat);
        DatVerifier.Verify(new Game { Path = path }, [index]).Verdict.Should().Be(DatVerdict.NotHashed);
    }
}
