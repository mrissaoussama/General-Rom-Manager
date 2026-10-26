using System.Buffers.Binary;
using AwesomeAssertions;
using RomManagerShared.ThreeDS;

namespace RomManagerTests;

public class SmdhIconTests
{
    private static byte[] Smdh() => new byte[0x36C0];

    private static void SetRaw(byte[] smdh, int index, ushort rgb565) =>
        BinaryPrimitives.WriteUInt16LittleEndian(smdh.AsSpan(SmdhIcon.LargeIconOffset + index * 2), rgb565);

    private static byte[] Pixel(byte[] rgba, int x, int y) => rgba.AsSpan((y * SmdhIcon.Size + x) * 4, 4).ToArray();

    [Fact]
    public void Decode_MapsMortonOrderWithinTilesAndRowMajorTiles()
    {
        var smdh = Smdh();
        SetRaw(smdh, 0, 0xF800);       // tile 0, k=0 -> (0,0) red
        SetRaw(smdh, 1, 0x07E0);       // k=1 -> (1,0) green
        SetRaw(smdh, 2, 0x001F);       // k=2 -> (0,1) blue
        SetRaw(smdh, 63, 0xFFFF);      // k=63 -> (7,7) white
        SetRaw(smdh, 64 + 5, 0xF800);  // tile 1 (x 8..15), k=5 = 0b000101 -> (8+3, 0)
        SetRaw(smdh, 6 * 64 + 10, 0x001F); // tile 6 (row 1), k=10 = 0b001010 -> (0, 8+3)

        var rgba = SmdhIcon.DecodeLarge(smdh)!;

        Pixel(rgba, 0, 0).Should().Equal(255, 0, 0, 255);
        Pixel(rgba, 1, 0).Should().Equal(0, 255, 0, 255);
        Pixel(rgba, 0, 1).Should().Equal(0, 0, 255, 255);
        Pixel(rgba, 7, 7).Should().Equal(255, 255, 255, 255);
        Pixel(rgba, 11, 0).Should().Equal(255, 0, 0, 255);
        Pixel(rgba, 0, 11).Should().Equal(0, 0, 255, 255);
        Pixel(rgba, 2, 0).Should().Equal(0, 0, 0, 255);
    }

    [Fact]
    public void Decode_ReturnsNullForBlankOrShortSmdh()
    {
        SmdhIcon.DecodeLarge(Smdh()).Should().BeNull();
        SmdhIcon.DecodeLarge(new byte[0x2040]).Should().BeNull();
    }

    [Fact]
    public void LargePng_ProducesUpscaledPng()
    {
        var smdh = Smdh();
        SetRaw(smdh, 0, 0xF800);
        var png = SmdhIcon.LargePng(smdh)!;
        png.AsSpan(1, 3).ToArray().Should().Equal((byte)'P', (byte)'N', (byte)'G');
        BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)).Should().Be(96); // IHDR width
    }
}
