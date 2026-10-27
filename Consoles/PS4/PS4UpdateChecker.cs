using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using RomManagerShared.Base;

namespace RomManagerShared.PS4;

/// <summary>
/// Latest PS4 patch versions from Sony's public update server (the XML the console itself reads):
/// <c>https://gs-sec.ww.np.dl.playstation.net/plo/np/{id}/{hmac}/{id}-ver.xml</c>, where hmac is
/// HMAC-SHA256 of <c>np_{id}</c> with the publicly documented update key. Online, so opt-in.
/// </summary>
public static class PS4UpdateChecker
{
    private static readonly byte[] UpdateKey = Convert.FromHexString("AD62E37F905E06BC19593142281C112CEC0E7EC3E97EFDCAEFCDBAAFA6378D84");

    // The server's chain ends at Sony's own root ("SCEI DNAS Root 05"), which no OS trusts; only that root is accepted.
    private const string SonyRootThumbprint = "F22923F218BAB9CD966A1FDEA3C0F434B8663A22";

    /// <summary>Allows the online check (Settings). Off by default: one request to Sony per game.</summary>
    public static bool OnlineAllowed { get; set; }

    public static string UpdateUrl(string titleId)
    {
        var id = titleId.ToUpperInvariant();
        var hash = Convert.ToHexStringLower(HMACSHA256.HashData(UpdateKey, Encoding.ASCII.GetBytes("np_" + id)));
        return $"https://gs-sec.ww.np.dl.playstation.net/plo/np/{id}/{hash}/{id}-ver.xml";
    }

    /// <summary>The newest package version in an update XML ("01.57"), or null when it lists none.</summary>
    public static string? ParseLatestVersion(string xml)
    {
        var versions = XDocument.Parse(xml).Descendants("package")
            .Select(p => (string?)p.Attribute("version"))
            .OfType<string>()
            .ToList();
        return versions.Count == 0 ? null : versions.MaxBy(ParseVersion);
    }

    public static int ParseVersion(string? version) =>
        int.TryParse((version ?? "").Replace(".", ""), NumberStyles.None, CultureInfo.InvariantCulture, out var v) ? v : 0;

    public static HttpClient CreateClient() => new(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, cert, chain, errors) => IsTrusted(cert, chain, errors),
    })
    { Timeout = TimeSpan.FromSeconds(20) };

    internal static bool IsTrusted(X509Certificate2? cert, X509Chain? chain, SslPolicyErrors errors)
    {
        if (errors == SslPolicyErrors.None) return true;
        if (errors != SslPolicyErrors.RemoteCertificateChainErrors || cert is null || chain is null) return false;
        // Rebuild the chain against Sony's root only.
        var root = chain.ChainElements.Select(e => e.Certificate).LastOrDefault();
        if (root is null || !string.Equals(root.Thumbprint, SonyRootThumbprint, StringComparison.OrdinalIgnoreCase)) return false;
        using var pinned = new X509Chain();
        pinned.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        pinned.ChainPolicy.CustomTrustStore.Add(root);
        pinned.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        return pinned.Build(cert);
    }

    /// <summary>
    /// Games whose newest installed version (the game or an update with the same title ID) is older than Sony's
    /// newest patch. <paramref name="fetch"/> returns the XML for a URL, or null when the title has no patches.
    /// </summary>
    public static async Task<List<MissingUpdate>> FindMissingAsync(IEnumerable<Rom> library, Func<string, CancellationToken, Task<string?>> fetch,
        CancellationToken ct = default)
    {
        var roms = library.Where(r => r is PS4Game or PS4Update && !string.IsNullOrEmpty(r.TitleID)).ToList();
        var installed = roms.GroupBy(r => r.TitleID!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.MaxBy(r => ParseVersion(r.Version))!.Version ?? "", StringComparer.OrdinalIgnoreCase);
        var missing = new List<MissingUpdate>();
        foreach (var game in roms.OfType<PS4Game>().DistinctBy(g => g.TitleID, StringComparer.OrdinalIgnoreCase))
        {
            ct.ThrowIfCancellationRequested();
            string? xml;
            try { xml = await fetch(UpdateUrl(game.TitleID!), ct); }
            catch (HttpRequestException ex)
            {
                AppLog.Warn($"[PS4] update check {game.TitleID}: {ex.Message}");
                continue;
            }
            if (xml is null || ParseLatestVersion(xml) is not { } latest) continue;
            var have = installed[game.TitleID!];
            if (ParseVersion(latest) > ParseVersion(have))
                missing.Add(new MissingUpdate(game, have, latest));
        }
        return [.. missing.OrderBy(m => m.Title, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>GETs an update XML; titles without patches answer 404.</summary>
    public static async Task<string?> FetchAsync(HttpClient http, string url, CancellationToken ct)
    {
        using var response = await http.GetAsync(url, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
}
