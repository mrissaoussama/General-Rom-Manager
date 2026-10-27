using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using RomManagerShared.Logging;
using RomManagerShared.Utils;

namespace RomManagerTests;

public class AppLogTests
{
    [Fact]
    public void Entries_CarryLevelAndTheCallingFileAsCategory()
    {
        var marker = Guid.NewGuid().ToString("N");
        var seen = new List<string>();
        void Collect(string e) { if (e.Contains(marker)) lock (seen) seen.Add(e); }
        LogSink.LogAdded += Collect;
        try
        {
            AppLog.Info($"{marker} info");
            AppLog.Warn($"{marker} warn");
            AppLog.Error($"{marker} error", new InvalidOperationException("boom"));
        }
        finally { LogSink.LogAdded -= Collect; }

        seen.Should().HaveCount(3);
        seen[0].Should().Contain("[AppLogTests]").And.NotContain("WARN");
        seen[1].Should().Contain("WARN [AppLogTests]");
        seen[2].Should().Contain("ERROR [AppLogTests]").And.Contain("boom");
    }

    [Fact]
    public async Task ParseDiagnostics_GetTheMessageWithoutDecoration()
    {
        var (_, messages) = await ParseDiagnostics.CaptureAsync(() =>
        {
            AppLog.Warn("[PS4] game.pkg: no readable param.sfo");
            return Task.FromResult(0);
        });

        messages.Should().ContainSingle().Which.Should().Be("[PS4] game.pkg: no readable param.sfo");
    }

    [Fact]
    public void For_UsesTheConfiguredFactory()
    {
        var fresh = AppLog.For("Test.Category");
        fresh.IsEnabled(LogLevel.Information).Should().BeTrue();
        fresh.IsEnabled(LogLevel.Debug).Should().BeFalse();
    }
}
