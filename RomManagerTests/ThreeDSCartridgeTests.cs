using System.Buffers.Binary;
using System.Text;
using AwesomeAssertions;
using RomManagerShared.ThreeDS;
using RomManagerShared.ThreeDS.Parsers;

namespace RomManagerTests;

public sealed class ThreeDSCartridgeTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("3ds").FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    private static void WriteNcch(byte[] image, int offset, long programId, string productCode, bool noCrypto)
    {
        Encoding.ASCII.GetBytes("NCCH").CopyTo(image, offset + 0x100);
        BinaryPrimitives.WriteInt64LittleEndian(image.AsSpan(offset + 0x118), programId);
        Encoding.ASCII.GetBytes(productCode).CopyTo(image, offset + 0x150);
        image[offset + 0x188 + 7] = (byte)(noCrypto ? 0x4 : 0);
    }

    private static byte[] Ncsd(long programId, string productCode, bool noCrypto)
    {
        var image = new byte[0x4200];
        Encoding.ASCII.GetBytes("NCSD").CopyTo(image, 0x100);
        BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(0x120), 0x20); // partition 0 at 0x4000
        WriteNcch(image, 0x4000, programId, productCode, noCrypto);
        return image;
    }

    [Fact]
    public void TryRead_FollowsTheNcsdPartitionTableToTheGameNcch()
    {
        var header = ThreeDSCartridgeHeader.TryRead(new MemoryStream(Ncsd(0x0004000000055D00, "CTR-P-AREE", noCrypto: false)));

        header.Should().Be(new ThreeDSCartridgeHeader(0x0004000000055D00, "CTR-P-AREE", Encrypted: true));
        header!.TitleId.Should().Be("0004000000055D00");
    }

    [Fact]
    public void TryRead_ReadsABareNcchAndTheNoCryptoFlag()
    {
        var image = new byte[0x200];
        WriteNcch(image, 0, 0x000400000F700000, "CTR-P-ABCD", noCrypto: true);

        ThreeDSCartridgeHeader.TryRead(new MemoryStream(image))!.Encrypted.Should().BeFalse();
    }

    [Fact]
    public void TryRead_RejectsOtherFiles() =>
        ThreeDSCartridgeHeader.TryRead(new MemoryStream(new byte[0x400])).Should().BeNull();

    [Fact]
    public async Task Parser_ReturnsTheTitleIdForEncryptedCartridges()
    {
        var path = Path.Combine(_dir, "game.3ds");
        File.WriteAllBytes(path, Ncsd(0x0004000000055D00, "CTR-P-AREE", noCrypto: false));

        var roms = await new ThreeDSCartridgeParser().ProcessFile(path);

        var game = roms.Should().ContainSingle().Which;
        game.Should().BeOfType<ThreeDSGame>();
        game.TitleID.Should().Be("0004000000055D00");
        game.ProductCode.Should().Be("CTR-P-AREE");
    }
}
