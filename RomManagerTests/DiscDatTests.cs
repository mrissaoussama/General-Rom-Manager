using System.Security.Cryptography;
using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Utils.Dat;

namespace RomManagerTests;

public sealed class DiscDatTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("disc").FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public async Task HashAsync_MatchesOneShotHashesAcrossBufferBoundaries()
    {
        var data = new byte[(1 << 20) * 2 + 123];
        new Random(42).NextBytes(data);
        var path = Path.Combine(_dir, "game.iso");
        File.WriteAllBytes(path, data);

        var (sha1, crc, size) = await PS2RedumpProvider.HashAsync(path, TestContext.Current.CancellationToken);

        sha1.Should().Be(Convert.ToHexStringLower(SHA1.HashData(data)));
        crc.Should().Be(System.IO.Hashing.Crc32.HashToUInt32(data).ToString("x8"));
        size.Should().Be(data.Length);
    }

    [Fact]
    public async Task Provider_DoesNothingUnlessEnabled()
    {
        var path = Path.Combine(_dir, "game.iso");
        File.WriteAllBytes(path, [1, 2, 3]);
        var rom = new Game { Path = path, TitleID = "SLUS_200.62" };

        PS2RedumpProvider.Enabled.Should().BeFalse();
        (await new PS2RedumpProvider().GetTitleInfo(rom)).Hashes.Should().BeNull();
        new PS2RedumpProvider().DataFingerprint.Should().BeEmpty();
    }
}
