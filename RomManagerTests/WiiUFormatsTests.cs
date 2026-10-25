using System.Buffers.Binary;
using System.Text;
using FluentAssertions;
using RomManagerShared.Base;
using RomManagerShared.WiiU;
using RomManagerShared.WiiU.Parsers;

namespace RomManagerTests;

public class WiiUFormatsTests
{
    private static string Temp(string ext) => Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.{ext}");

    [Fact]
    public void ReadTmd_Rsa2048_ReadsTitleIdAndVersion()
    {
        var tmd = new byte[0x200];
        BinaryPrimitives.WriteUInt32BigEndian(tmd, 0x10004);
        BinaryPrimitives.WriteUInt64BigEndian(tmd.AsSpan(0x18C), 0x0005000E10144000);
        BinaryPrimitives.WriteUInt16BigEndian(tmd.AsSpan(0x1DC), 16);
        var path = Temp("tmd");
        File.WriteAllBytes(path, tmd);
        try
        {
            WiiUFormats.ReadTmd(path).Should().Be(("0005000E10144000", (ushort)16));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadTmd_UnknownSignature_ReturnsNull()
    {
        var path = Temp("tmd");
        File.WriteAllBytes(path, new byte[0x200]);
        try
        {
            WiiUFormats.ReadTmd(path).Should().BeNull();
        }
        finally
        {
            File.Delete(path);
        }
    }

    // WUX layout: 0x20 header, u32 index per sector, sectors aligned to sector size.
    private static byte[] BuildWux(string sectorZeroText)
    {
        const int sectorSize = 0x8000;
        const int sectorCount = 4;
        var dataOffset = sectorSize; // header + index fit in the first sector
        var wux = new byte[dataOffset + 2 * sectorSize];
        var s = wux.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(s, 0x30585557);
        BinaryPrimitives.WriteUInt32LittleEndian(s[4..], 0x1099D02E);
        BinaryPrimitives.WriteUInt32LittleEndian(s[8..], sectorSize);
        BinaryPrimitives.WriteUInt64LittleEndian(s[0x10..], (ulong)sectorCount * sectorSize);
        // Sector 0 is stored second to prove the index is followed.
        uint[] index = [1, 0, 0, 0];
        for (int i = 0; i < sectorCount; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(s[(0x20 + i * 4)..], index[i]);
        Encoding.ASCII.GetBytes(sectorZeroText).CopyTo(wux, dataOffset + sectorSize);
        return wux;
    }

    [Fact]
    public async Task WudWuxParser_ReadsProductCodeFromWux()
    {
        var path = Temp("wux");
        File.WriteAllBytes(path, BuildWux("WUP-P-ADVE-00-301USA-0"));
        try
        {
            var roms = await new WiiUWudWuxParser().ProcessFile(path);

            var rom = roms.Should().ContainSingle().Subject;
            rom.ProductCode.Should().Be("WUP-P-ADVE");
            rom.Regions.Should().Equal(Region.USA);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WudWuxParser_ReadsProductCodeFromWud()
    {
        var path = Temp("wud");
        File.WriteAllBytes(path, Encoding.ASCII.GetBytes("WUP-P-AFDP-00-000000-0".PadRight(0x40, '\0')));
        try
        {
            var roms = await new WiiUWudWuxParser().ProcessFile(path);

            roms.Should().ContainSingle().Which.Regions.Should().Equal(Region.Europe);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WudWuxParser_NonWiiUFile_ReturnsEmpty()
    {
        var path = Temp("wud");
        File.WriteAllBytes(path, new byte[0x40]);
        try
        {
            (await new WiiUWudWuxParser().ProcessFile(path)).Should().BeEmpty();
        }
        finally
        {
            File.Delete(path);
        }
    }

    // WUD with an AES-CBC (zero IV) encrypted partition table at 0x18000.
    private static byte[] BuildKeyedWud(byte[] key, params string[] partitionNames)
    {
        var wud = new byte[0x18000 + 0x8000];
        Encoding.ASCII.GetBytes("WUP-P-ADVE-00-301USA-0").CopyTo(wud, 0);
        var table = new byte[0x8000];
        BinaryPrimitives.WriteUInt32BigEndian(table, 0xCCA6E67B);
        BinaryPrimitives.WriteUInt32BigEndian(table.AsSpan(0x1C), (uint)partitionNames.Length);
        for (int i = 0; i < partitionNames.Length; i++)
            Encoding.ASCII.GetBytes(partitionNames[i]).CopyTo(table, 0x800 + i * 0x80);
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = key;
        aes.EncryptCbc(table, new byte[16], System.Security.Cryptography.PaddingMode.None).CopyTo(wud, 0x18000);
        return wud;
    }

    [Fact]
    public async Task WudWuxParser_KeyInFileName_ReadsGamePartitionTitleIds()
    {
        var key = Convert.FromHexString("FA005119B635F34AFEAB081FD394F787");
        var dir = Directory.CreateTempSubdirectory().FullName;
        var path = Path.Combine(dir, "Adventure Time (USA) [ADVEG9] [FA005119B635F34AFEAB081FD394F787].wud");
        File.WriteAllBytes(path, BuildKeyedWud(key, "SI", "UP0005001010046100000000007D00",
            "GM0005000010144000000000000000", "GM0005001010060000000000000000"));
        try
        {
            var rom = (await new WiiUWudWuxParser().ProcessFile(path)).Should().ContainSingle().Subject;

            rom.TitleID.Should().Be("0005000010144000"); // system partition (00050010) skipped
            rom.ProductCode.Should().Be("WUP-P-ADVE");
            rom.Titles![0].Value.Should().Be("Adventure Time (USA)");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task WudWuxParser_SiblingKeyFile_IsUsed()
    {
        var key = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var path = Path.Combine(dir, "game.wud");
        File.WriteAllBytes(path, BuildKeyedWud(key, "GM0005000010162800000000000000"));
        File.WriteAllBytes(Path.Combine(dir, "game.key"), key);
        try
        {
            (await new WiiUWudWuxParser().ProcessFile(path)).Should().ContainSingle()
                .Which.TitleID.Should().Be("0005000010162800");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task WudWuxParser_WrongKey_FallsBackToProductCode()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        var path = Path.Combine(dir, $"Game [{new string('0', 32)}].wud");
        File.WriteAllBytes(path, BuildKeyedWud(Enumerable.Repeat((byte)7, 16).ToArray(), "GM0005000010162800000000000000"));
        try
        {
            var rom = (await new WiiUWudWuxParser().ProcessFile(path)).Should().ContainSingle().Subject;
            rom.TitleID.Should().BeNull();
            rom.ProductCode.Should().Be("WUP-P-ADVE");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
