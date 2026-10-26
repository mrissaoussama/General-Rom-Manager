using AwesomeAssertions;
using RomManagerShared.Switch;

namespace RomManagerTests;

public class SwitchKeyStatusTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"keys_{Guid.NewGuid():N}");

    public SwitchKeyStatusTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Write(string name, params string[] lines)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllLines(path, lines);
        return path;
    }

    private static string FakeHex(int bytes) => new('a', bytes * 2);

    [Fact]
    public void Read_CountsKeysAndFindsHighestMasterKey()
    {
        var prod = Write("prod.keys",
            $"header_key = {FakeHex(32)}",
            $"master_key_00 = {FakeHex(16)}",
            $"master_key_0a = {FakeHex(16)}",
            $"master_key_03 = {FakeHex(16)}",
            "; comment = 00",
            "",
            "broken line");
        var title = Write("title.keys",
            $"01000000000000000000000000000000 = {FakeHex(16)}",
            $"0100A970164A80000000000000000000={FakeHex(16)}");

        var status = SwitchKeyStatus.Read(prod, title);

        status.ProdKeys.Exists.Should().BeTrue();
        status.ProdKeys.KeyCount.Should().Be(4);
        status.HighestMasterKey.Should().Be(0x0a);
        status.FirmwareHint.Should().Be("9.1.0");
        status.TitleKeys.Exists.Should().BeTrue();
        status.TitleKeys.KeyCount.Should().Be(2);
    }

    [Fact]
    public void Read_MissingFiles_ReportsNotFound()
    {
        var status = SwitchKeyStatus.Read(Path.Combine(_dir, "nope.keys"), Path.Combine(_dir, "nope2.keys"));

        status.ProdKeys.Exists.Should().BeFalse();
        status.ProdKeys.KeyCount.Should().Be(0);
        status.TitleKeys.Exists.Should().BeFalse();
        status.HighestMasterKey.Should().BeNull();
        status.FirmwareHint.Should().BeNull();
    }

    [Fact]
    public void Read_IgnoresNonHexValuesAndMalformedMasterKeyNames()
    {
        var prod = Write("prod.keys",
            "master_key_zz = 0011",
            $"master_key_1 = {FakeHex(16)}",
            "master_key_05 = not-hex",
            $"master_key_7f = {FakeHex(16)}");

        var status = SwitchKeyStatus.Read(prod, Path.Combine(_dir, "missing"));

        status.ProdKeys.KeyCount.Should().Be(3); // zz, 1 and 7f have hex values
        status.HighestMasterKey.Should().Be(0x7f);
        status.FirmwareHint.Should().BeNull(); // beyond the known table
    }

    [Fact]
    public void Summary_DoesNotExposeKeyValues()
    {
        var secret = "deadbeef" + FakeHex(12);
        var prod = Write("prod.keys", $"master_key_00 = {secret}");

        var status = SwitchKeyStatus.Read(prod, prod);

        status.ProdKeys.ToString().Should().NotContain(secret);
        status.TitleKeys.ToString().Should().NotContain(secret);
    }
}
