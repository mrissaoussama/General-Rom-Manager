using AwesomeAssertions;
using RomManagerShared.Utils;

namespace RomManagerTests;

public sealed class ToolConverterTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("convert").FullName;
    private string Temp => Path.Combine(_dir, "temp");

    public void Dispose() => Directory.Delete(_dir, true);

    // A shell command or script stands in for chdman & co. ($1 = input, $2 = output on Unix.)
    private (ConversionTool Tool, string Exe) Shell(string windows, string unix)
    {
        if (OperatingSystem.IsWindows())
            return (new ConversionTool("test", "Test", "cmd", ["iso"], "chd", "/c " + windows), Path.Combine(Environment.SystemDirectory, "cmd.exe"));
        var script = Path.Combine(_dir, "tool.sh");
        File.WriteAllText(script, $"#!/bin/sh\n{unix}\n");
        File.SetUnixFileMode(script, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return (new ConversionTool("test", "Test", "tool", ["iso"], "chd", "\"{in}\" \"{out}\""), script);
    }

    [Fact]
    public async Task Converts_KeepsTheOriginal_AndCleansTheTempFolder()
    {
        var input = Path.Combine(_dir, "game.iso");
        File.WriteAllText(input, "data");
        var (tool, exe) = Shell("copy \"{in}\" \"{out}\"", "cp \"$1\" \"$2\"");

        var output = await ToolConverter.ConvertAsync(tool, exe, input, TestContext.Current.CancellationToken, Temp);

        output.Should().Be(Path.Combine(_dir, "game.chd"));
        File.ReadAllText(output).Should().Be("data");
        File.Exists(input).Should().BeTrue();
        Directory.EnumerateFileSystemEntries(Temp).Should().BeEmpty();
    }

    [Fact]
    public async Task FailingTool_ReportsItsOutput_AndWritesNothing()
    {
        var input = Path.Combine(_dir, "game.iso");
        File.WriteAllText(input, "data");
        var (tool, exe) = Shell("echo broken disc && exit 3", "echo broken disc; exit 3");

        var act = () => ToolConverter.ConvertAsync(tool, exe, input, TestContext.Current.CancellationToken, Temp);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("exit 3").And.Contain("broken disc");
        File.Exists(Path.Combine(_dir, "game.chd")).Should().BeFalse();
    }

    [Fact]
    public async Task ExistingOutput_IsNeverOverwritten()
    {
        var input = Path.Combine(_dir, "game.iso");
        File.WriteAllText(input, "data");
        File.WriteAllText(Path.Combine(_dir, "game.chd"), "mine");
        var (tool, exe) = Shell("exit 0", "exit 0");

        var act = () => ToolConverter.ConvertAsync(tool, exe, input, TestContext.Current.CancellationToken, Temp);

        await act.Should().ThrowAsync<IOException>();
        File.ReadAllText(Path.Combine(_dir, "game.chd")).Should().Be("mine");
    }

    [Fact]
    public void Catalog_MatchesInputExtensions()
    {
        ConversionTools.Find("chdman-cd")!.Accepts("x.CUE").Should().BeTrue();
        ConversionTools.Find("dolphin-rvz")!.Accepts("x.rvz").Should().BeFalse();
    }
}
