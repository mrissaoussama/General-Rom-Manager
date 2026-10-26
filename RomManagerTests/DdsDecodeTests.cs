using AwesomeAssertions;
using RomManagerShared.Utils.ISO2GOD.Chilano.Xbox360.Graphics;

namespace RomManagerTests;

public class DdsDecodeTests
{
    [Fact]
    public void GetRgba_Argb_SwapsBgraToRgba()
    {
        var dds = new DDS(DDSType.ARGB);
        dds.SetDetails(1, 2, 1);
        dds.Data = [0x10, 0x20, 0x30, 0x40, 0x01, 0x02, 0x03, 0xFF]; // B G R A per pixel

        dds.GetRgba(DDSType.ARGB).Should().Equal(0x30, 0x20, 0x10, 0x40, 0x03, 0x02, 0x01, 0xFF);
    }

    [Fact]
    public void GetRgba_Dxt1_DecodesBlockColours()
    {
        var dds = new DDS(DDSType.DXT1);
        dds.SetDetails(4, 4, 1);
        // color0 = pure red (0xF800), color1 = pure blue (0x001F); row 0 uses index 1, other rows index 0.
        dds.Data = [0x00, 0xF8, 0x1F, 0x00, 0b01010101, 0, 0, 0];

        var rgba = dds.GetRgba(DDSType.DXT1);

        rgba.Should().HaveCount(4 * 4 * 4);
        rgba[..4].Should().Equal(0, 0, 255, 255);               // (0,0) blue
        rgba[(4 * 4)..(4 * 4 + 4)].Should().Equal(255, 0, 0, 255); // (0,1) red
    }
}
