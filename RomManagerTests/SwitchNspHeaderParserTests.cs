using System.Text;
using FluentAssertions;
using RomManagerShared.Switch.Parsers;

namespace RomManagerTests;

public class SwitchNspHeaderParserTests
{
    private static string WriteNsp(string header)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.nsp");
        // Non-ASCII bytes before the names must not shift offsets.
        byte[] junk = [0x50, 0x46, 0x53, 0x30, 0xC3, 0xFF, 0x80, 0x00];
        File.WriteAllBytes(path, [.. junk, .. Encoding.ASCII.GetBytes(header)]);
        return path;
    }

    [Fact]
    public async Task ProcessFile_ReadsTitleIdFromTicketName()
    {
        var path = WriteNsp("abc.nca\00100A970164A8000000000000000000E.tik\0");
        try
        {
            var roms = await new SwitchRomNSPNSZParser().ProcessFile(path);

            roms.Should().ContainSingle().Which.TitleID.Should().Be("0100A970164A8000");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ProcessFile_ReadsContentMetaXml()
    {
        var path = WriteNsp("<ContentMeta><Type>Patch</Type><Id>0x0100a970164a8800</Id><Version>131072</Version>" +
                            "<RequiredSystemVersion>0</RequiredSystemVersion></ContentMeta>");
        try
        {
            var roms = await new SwitchRomNSPNSZParser().ProcessFile(path);

            var rom = roms.Should().ContainSingle().Subject;
            rom.TitleID.Should().Be("0100A970164A8800");
            rom.Version.Should().Be("131072");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ProcessFile_IgnoresNonHexNames()
    {
        var path = WriteNsp("not-a-rights-id-name-at-all-xx.tik\0");
        try
        {
            (await new SwitchRomNSPNSZParser().ProcessFile(path)).Should().BeEmpty();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
