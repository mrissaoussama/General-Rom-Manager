using AwesomeAssertions;
using RomManagerShared.Utils;

namespace RomManagerTests;

public class EmulatorLauncherTests
{
    [Fact]
    public void BuildStartInfo_ReplacesTokens()
    {
        var rom = Path.Combine(Path.GetTempPath(), "roms", "Zelda (USA).nsp");
        var exe = Path.Combine(Path.GetTempPath(), "emu", "ryujinx");

        var info = EmulatorLauncher.BuildStartInfo(exe, "--fullscreen \"{rom}\" --name {name} --dir \"{dir}\"", rom);

        info.FileName.Should().Be(exe);
        info.Arguments.Should().Be($"--fullscreen \"{rom}\" --name Zelda (USA) --dir \"{Path.GetDirectoryName(rom)}\"");
        info.UseShellExecute.Should().BeFalse();
        info.WorkingDirectory.Should().Be(Path.GetDirectoryName(exe));
    }

    [Fact]
    public void BuildStartInfo_DefaultsToTheQuotedRomPath() =>
        EmulatorLauncher.BuildStartInfo("emu", " ", "a b.iso").Arguments.Should().Be("\"a b.iso\"");

    [Fact]
    public void Launch_RejectsAMissingEmulator() =>
        FluentActions.Invoking(() => EmulatorLauncher.Launch(Path.Combine(Path.GetTempPath(), "missing-emu.exe"), null, "x.iso"))
            .Should().Throw<FileNotFoundException>();
}
