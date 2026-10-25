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
}
