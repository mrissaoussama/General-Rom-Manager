using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace RomManagerShared.Utils;

/// <summary>
/// Resolves rom images (local paths or http(s) URLs) to local files. URLs are downloaded once into
/// <see cref="Directory"/> and reused across runs.
/// </summary>
public sealed class ImageCache(string directory, HttpClient http)
{
    private readonly ConcurrentDictionary<string, Task<string?>> _pending = new();

    public string Directory { get; } = directory;

    public Task<string?> GetAsync(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return Task.FromResult<string?>(null);
        if (File.Exists(source))
            return Task.FromResult<string?>(source);
        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            return Task.FromResult<string?>(null);

        var target = Path.Combine(Directory, CacheName(uri));
        if (File.Exists(target))
            return Task.FromResult<string?>(target);
        return _pending.GetOrAdd(target, _ => DownloadAsync(uri, target));
    }

    private async Task<string?> DownloadAsync(Uri uri, string target)
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            var bytes = await http.GetByteArrayAsync(uri);
            var tmp = target + ".part";
            await File.WriteAllBytesAsync(tmp, bytes);
            File.Move(tmp, target, overwrite: true);
            return target;
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[Image] {uri}: {ex.Message}");
            return null; // stays cached as a miss for this session
        }
    }

    private static string CacheName(Uri uri)
    {
        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(uri.AbsoluteUri)))[..20].ToLowerInvariant();
        var ext = Path.GetExtension(uri.AbsolutePath);
        if (ext.Length is < 2 or > 5) ext = ".img";
        return hash + ext.ToLowerInvariant();
    }
}
