using System.Buffers.Binary;
using System.Text;
using FluentAssertions;
using RomManagerShared.DS;

namespace RomManagerTests;

public class DSBannerTests
{
    private static ushort Crc16(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++) crc = (ushort)((crc & 1) != 0 ? crc >> 1 ^ 0xA001 : crc >> 1);
        }
        return crc;
    }

    private static byte[] Rom(string englishTitle, bool corruptCrc = false)
    {
        var rom = new byte[0x2000];
        BinaryPrimitives.WriteUInt32LittleEndian(rom.AsSpan(0x68), 0x1000);
        var block = rom.AsSpan(0x1000, 0x840);
        block[0] = 1;
        block.Slice(0x20, 0x200).Fill(0x11);                        // every pixel uses colour 1
        BinaryPrimitives.WriteUInt16LittleEndian(block[0x222..], 0x001F); // colour 1 = red
        BinaryPrimitives.WriteUInt16LittleEndian(block[0x224..], 0x7C00); // colour 2 = blue
        Encoding.Unicode.GetBytes(englishTitle).CopyTo(block[0x340..]);
        var crc = Crc16(block.Slice(0x20, 0x820));
        BinaryPrimitives.WriteUInt16LittleEndian(block[2..], (ushort)(corruptCrc ? crc ^ 1 : crc));
        return rom;
    }

    [Fact]
    public void Read_DecodesTitlePublisherAndIcon()
    {
        var banner = DSBanner.Read(new MemoryStream(Rom("Kirby\nCanvas Curse\nNintendo")))!;

        banner.Title.Should().Be("Kirby Canvas Curse");
        banner.Publisher.Should().Be("Nintendo");
        banner.IconRgba!.AsSpan(0, 4).ToArray().Should().Equal(255, 0, 0, 255);
        banner.IconPng()!.AsSpan(1, 3).ToArray().Should().Equal((byte)'P', (byte)'N', (byte)'G');
    }

    [Fact]
    public void Read_RejectsBadChecksum() =>
        DSBanner.Read(new MemoryStream(Rom("Game\nPub", corruptCrc: true))).Should().BeNull();
}
