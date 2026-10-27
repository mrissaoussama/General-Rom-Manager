using System.Net;
using AwesomeAssertions;
using RomManagerShared.Utils;

namespace RomManagerTests;

public sealed class ImageCacheTests : IDisposable
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public int Calls;
        public HttpStatusCode Status = HttpStatusCode.OK;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref Calls);
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new ByteArrayContent([1, 2, 3]) });
        }
    }

    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;
    private readonly StubHandler _handler = new();
    private ImageCache NewCache() => new(_dir, new HttpClient(_handler));

    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public async Task GetAsync_DownloadsUrlOnce_ThenServesFromDisk()
    {
        const string url = "https://example.com/art/icon.PNG";

        var first = await NewCache().GetAsync(url);
        var again = await NewCache().GetAsync(url); // new instance: hits the file, not the network

        first.Should().NotBeNull().And.EndWith(".png");
        again.Should().Be(first);
        File.ReadAllBytes(first!).Should().Equal(1, 2, 3);
        _handler.Calls.Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_ReturnsLocalFilesAsIs_AndIgnoresUnknownSources()
    {
        var local = Path.Combine(_dir, "local.jpg");
        File.WriteAllBytes(local, [9]);
        var cache = NewCache();

        (await cache.GetAsync(local)).Should().Be(local);
        (await cache.GetAsync("missing/file.png")).Should().BeNull();
        (await cache.GetAsync("ftp://example.com/a.png")).Should().BeNull();
        (await cache.GetAsync(null)).Should().BeNull();
        _handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task GetAsync_FailedDownload_ReturnsNull()
    {
        _handler.Status = HttpStatusCode.NotFound;

        (await NewCache().GetAsync("https://example.com/none.png")).Should().BeNull();
        Directory.GetFiles(_dir).Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_WithDownloadsOff_UsesOnlyWhatIsCached()
    {
        const string url = "https://example.com/art/cover.png";
        await NewCache().GetAsync(url);
        var offline = NewCache();
        offline.AllowDownloads = false;

        (await offline.GetAsync(url)).Should().NotBeNull(); // already on disk
        (await offline.GetAsync("https://example.com/art/other.png")).Should().BeNull();
        _handler.Calls.Should().Be(1);
    }
}
