using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.PS4;

namespace RomManagerTests;

public class PS4UpdateCheckerTests
{
    private const string Xml = """
        <?xml version='1.0' encoding='UTF-8'?>
        <titlepatch titleid="CUSA00419"><tag name="67" mandatory="true">
          <package version="01.09" size="1" />
          <package version="01.57" size="2" />
        </tag></titlepatch>
        """;

    [Fact]
    public void UpdateUrl_UsesTheHmacOfTheTitleId() =>
        PS4UpdateChecker.UpdateUrl("cusa00419").Should().Be(
            "https://gs-sec.ww.np.dl.playstation.net/plo/np/CUSA00419/d5b7a32456606df00438f055e5af051b4eed76427ec299d9da6f5b13b0996c1f/CUSA00419-ver.xml");

    [Fact]
    public void ParseLatestVersion_TakesTheHighestPackage()
    {
        PS4UpdateChecker.ParseLatestVersion(Xml).Should().Be("01.57");
        PS4UpdateChecker.ParseLatestVersion("<titlepatch/>").Should().BeNull();
    }

    [Fact]
    public async Task FindMissing_ComparesWithTheNewestInstalledVersion()
    {
        PS4Game Game(string id, string version, string title)
        {
            var g = new PS4Game { TitleID = id, Version = version };
            g.AddTitleName(title);
            return g;
        }
        var behind = Game("CUSA00419", "01.00", "Behind");
        var patched = Game("CUSA00001", "01.00", "Patched");
        var patch = new PS4Update { TitleID = "CUSA00001", Version = "01.57" };
        var noPatches = Game("CUSA09999", "01.00", "No patches");
        var asked = new List<string>();

        var missing = await PS4UpdateChecker.FindMissingAsync([behind, patched, patch, noPatches], (url, _) =>
        {
            asked.Add(url);
            return Task.FromResult(url.Contains("CUSA09999") ? null : Xml);
        }, TestContext.Current.CancellationToken);

        asked.Should().HaveCount(3);
        var m = missing.Should().ContainSingle().Subject;
        m.Game.Should().BeSameAs(behind);
        (m.InstalledVersion, m.LatestVersion).Should().Be(("01.00", "01.57"));
    }

    [Fact]
    public void Tls_RejectsChainsNotEndingAtSonysRoot() =>
        PS4UpdateChecker.IsTrusted(null, null, System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors).Should().BeFalse();

    // Network: runs only with LIVE_NETWORK=1 (checks the certificate pin and the XML against Sony's server).
    [Fact]
    public async Task Live_SonyServerAnswersThroughThePinnedRoot()
    {
        if (Environment.GetEnvironmentVariable("LIVE_NETWORK") != "1") return;
        using var http = PS4UpdateChecker.CreateClient();

        var xml = await PS4UpdateChecker.FetchAsync(http, PS4UpdateChecker.UpdateUrl("CUSA00419"), TestContext.Current.CancellationToken);

        PS4UpdateChecker.ParseVersion(PS4UpdateChecker.ParseLatestVersion(xml!)).Should().BeGreaterThan(100);
    }
}
