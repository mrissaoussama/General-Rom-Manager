using System.Buffers.Binary;
using FluentAssertions;
using RomManagerShared.PSP;
using RomManagerShared.PSP.Parsers;
using RomManagerShared.Utils;

namespace RomManagerTests;

public class PbpTests
{
    private static byte[] BuildPbp(byte[] sfo, byte[] icon)
    {
        var header = 0x28;
        var pbp = new byte[header + sfo.Length + icon.Length + 4];
        var s = pbp.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(s, PbpReader.Magic);
        BinaryPrimitives.WriteUInt32LittleEndian(s[4..], 0x10000);
        var offsets = new[] { header, header + sfo.Length, header + sfo.Length + icon.Length };
        BinaryPrimitives.WriteUInt32LittleEndian(s[8..], (uint)offsets[0]);
        BinaryPrimitives.WriteUInt32LittleEndian(s[12..], (uint)offsets[1]);
        for (int i = 2; i < 8; i++) BinaryPrimitives.WriteUInt32LittleEndian(s[(8 + i * 4)..], (uint)offsets[2]);
        sfo.CopyTo(pbp, offsets[0]);
        icon.CopyTo(pbp, offsets[1]);
        return pbp;
    }

    [Fact]
    public void Read_ReturnsSfoAndIcon()
    {
        var sfo = SfoTestData.Build(("TITLE", "Patapon"), ("TITLE_ID", "UCES00995"));
        byte[] icon = [0x89, (byte)'P', (byte)'N', (byte)'G', 1, 2, 3, 4, 5, 6];

        var pbp = PbpReader.Read(new MemoryStream(BuildPbp(sfo, icon)))!;

        pbp.Sfo!.GetString("TITLE").Should().Be("Patapon");
        pbp.Icon0.Should().Equal(icon);
    }

    [Fact]
    public void Read_RejectsOtherFiles() =>
        PbpReader.Read(new MemoryStream(new byte[0x40])).Should().BeNull();

    [Fact]
    public async Task PspParser_ReadsEbootPbp()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pbp");
        File.WriteAllBytes(path, BuildPbp(SfoTestData.Build(("TITLE", "Final Fantasy VII"), ("DISC_ID", "SCUS94163"), ("CATEGORY", "ME")), [1, 2, 3]));
        try
        {
            var rom = (await new PSPRomParser().ProcessFile(path)).Should().ContainSingle().Which.Should().BeOfType<PSPGame>().Subject;
            rom.TitleID.Should().Be("SCUS94163");
            rom.Category.Should().Be("ME");
            rom.Titles![0].Value.Should().Be("Final Fantasy VII");
        }
        finally { File.Delete(path); }
    }
}
