using System.Buffers;
using System.Security.Cryptography;
using RomManagerShared.Base;

namespace RomManagerShared.Utils;

public static class HashUtils
{
    /// <summary>What "Calculate hashes" computes.</summary>
    public static readonly IReadOnlyList<HashTypeEnum> DefaultTypes = [HashTypeEnum.CRC32, HashTypeEnum.MD5, HashTypeEnum.SHA1];

    private const int BufferSize = 1 << 20;

    /// <summary>
    /// Hashes a rom's file, or every file of a folder-format rom, and puts the results on <see cref="Rom.Hashes"/>
    /// (replacing earlier local hashes of the same file and type).
    /// </summary>
    public static async Task<List<RomHash>> CalculateRomHashes(Rom rom, IEnumerable<HashTypeEnum> hashTypes,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rom);
        if (string.IsNullOrEmpty(rom.Path)) return [];
        var hashes = rom.IsFolderFormat
            ? await CalculateRomFolderHashes(rom, hashTypes, progress, ct)
            : await CalculateFileHashes(rom.Path, hashTypes, progress, ct);

        rom.Hashes ??= [];
        rom.Hashes.RemoveAll(old => !old.IsVerified && hashes.Any(h => h.Type == old.Type && h.Filename == old.Filename && h.Extension == old.Extension));
        rom.Hashes.AddRange(hashes);
        return hashes;
    }

    /// <summary>Hashes every file under a folder-format rom's base folder, subfolders included.</summary>
    public static async Task<List<RomHash>> CalculateRomFolderHashes(Rom rom, IEnumerable<HashTypeEnum> hashTypes,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(rom.Path)) throw new ArgumentException("missing path", nameof(rom));
        var types = hashTypes.ToList();
        var files = Directory.EnumerateFiles(RomUtils.GetRomBaseFolder(rom), "*", SearchOption.AllDirectories)
            .Select(f => new FileInfo(f)).ToList();
        long total = Math.Max(1, files.Sum(f => f.Length)), before = 0;
        List<RomHash> hashlist = [];
        foreach (var file in files)
        {
            var offset = before;
            var fileProgress = progress is null ? null : new SyncProgress(p => progress.Report((offset + p / 100 * file.Length) * 100 / total));
            hashlist.AddRange(await CalculateFileHashes(file.FullName, types, fileProgress, ct));
            before += file.Length;
        }
        return hashlist;
    }

    /// <summary>Computes every requested hash in one read of the file. Values are upper-case hex.</summary>
    public static async Task<List<RomHash>> CalculateFileHashes(string path, IEnumerable<HashTypeEnum> hashTypes,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(path)) throw new ArgumentException("missing path", nameof(path));
        var types = hashTypes.Distinct().ToList();

        var crc = types.Contains(HashTypeEnum.CRC32) ? new System.IO.Hashing.Crc32() : null;
        var incremental = types.Where(t => t != HashTypeEnum.CRC32)
            .ToDictionary(t => t, t => IncrementalHash.CreateHash(t switch
            {
                HashTypeEnum.MD5 => HashAlgorithmName.MD5,
                HashTypeEnum.SHA1 => HashAlgorithmName.SHA1,
                HashTypeEnum.SHA256 => HashAlgorithmName.SHA256,
                _ => throw new ArgumentOutOfRangeException(nameof(hashTypes), t, "Unsupported hash type."),
            }));
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            long length = Math.Max(1, stream.Length), done = 0;
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, BufferSize), ct)) > 0)
            {
                var chunk = buffer.AsSpan(0, read);
                crc?.Append(chunk);
                foreach (var hash in incremental.Values) hash.AppendData(chunk);
                done += read;
                progress?.Report(done * 100.0 / length);
            }

            var now = DateTime.Now;
            return [.. types.Select(t => new RomHash
            {
                Type = t,
                Value = t == HashTypeEnum.CRC32
                    ? crc!.GetCurrentHashAsUInt32().ToString("X8")
                    : Convert.ToHexString(incremental[t].GetHashAndReset()),
                CreationDate = now,
                Description = "Local",
                Extension = Path.GetExtension(path).TrimStart('.'),
                Filename = Path.GetFileNameWithoutExtension(path),
            })];
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            foreach (var hash in incremental.Values) hash.Dispose();
        }
    }

    /// <summary>
    /// Hashes each rom with <see cref="DefaultTypes"/> and hands the results to <paramref name="save"/>. A rom that
    /// fails is logged and skipped; progress covers the whole list. Returns how many roms were hashed.
    /// </summary>
    public static async Task<int> HashRomsAsync(IReadOnlyList<Rom> roms, Func<List<RomHash>, Task>? save,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        int index = 0, hashed = 0;
        foreach (var rom in roms)
        {
            ct.ThrowIfCancellationRequested();
            var start = index++ * 100.0 / roms.Count;
            if (string.IsNullOrEmpty(rom.Path) || !(rom.IsFolderFormat ? Directory.Exists(RomUtils.GetRomBaseFolder(rom)) : File.Exists(rom.Path)))
                continue;
            try
            {
                var each = progress is null ? null : new SyncProgress(p => progress.Report(start + p / roms.Count));
                var hashes = await CalculateRomHashes(rom, DefaultTypes, each, ct);
                if (save is not null && hashes.Count > 0)
                {
                    try { await save(hashes); }
                    catch (Exception ex) { AppLog.Warn($"[Hash] DB save failed for '{rom.Path}': {ex.Message}"); }
                }
                hashed++;
                AppLog.Info($"[Hash] {rom.Titles?.FirstOrDefault()?.Value ?? rom.TitleID}: {hashes.FirstOrDefault()?.Value ?? "(none)"}");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                AppLog.Warn($"[Hash] Error for '{rom.Path}': {ex.Message}");
            }
        }
        progress?.Report(100);
        return hashed;
    }

    // Progress<T> posts to the captured context; nested progress must report straight through.
    private sealed class SyncProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value) => report(value);
    }
}
