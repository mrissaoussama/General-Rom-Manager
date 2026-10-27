using AwesomeAssertions;
using RomManagerShared.Utils;

namespace RomManagerTests;

public sealed class HashCacheTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("hashcache").FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public async Task GetOrCompute_ReusesUntilTheFileChanges()
    {
        var file = Path.Combine(_dir, "disc.iso");
        File.WriteAllText(file, "abc");
        var store = Path.Combine(_dir, "hashes.json");
        int calls = 0;
        Task<(string, string, long)> Compute(string f) { calls++; return Task.FromResult(("sha" + calls, "crc", new FileInfo(f).Length)); }

        (await new HashCache(store).GetOrComputeAsync(file, Compute)).Sha1.Should().Be("sha1");
        (await new HashCache(store).GetOrComputeAsync(file, Compute)).Sha1.Should().Be("sha1"); // from disk
        calls.Should().Be(1);

        File.WriteAllText(file, "abcd");
        (await new HashCache(store).GetOrComputeAsync(file, Compute)).Sha1.Should().Be("sha2");
    }
}
