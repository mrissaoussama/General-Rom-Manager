namespace RomManagerShared.Base.Library;

/// <summary>
/// Watches a ROM folder and raises <see cref="Changed"/> once activity settles. Network shares can drop
/// events, so folders on network drives are also polled: a cheap listing (count, total size, newest
/// write time) is compared every <c>pollInterval</c>. The incremental scan stays the source of truth.
/// </summary>
public sealed class LibraryWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly HashSet<string> _extensions;
    private readonly Timer _debounce;
    private readonly TimeSpan _delay;
    private readonly Timer? _poll;
    private (int Count, long Size, long Newest)? _lastSnapshot;

    public string Root { get; }
    public event Action? Changed;

    public bool IsPolling => _poll is not null;

    /// <param name="pollInterval">Poll interval; default 2 minutes on network drives, off elsewhere.
    /// Pass <see cref="TimeSpan.Zero"/> to disable or a value to force polling.</param>
    public LibraryWatcher(string root, IEnumerable<string> extensions, TimeSpan? delay = null, TimeSpan? pollInterval = null)
    {
        Root = root;
        _delay = delay ?? TimeSpan.FromSeconds(3);
        _extensions = new HashSet<string>(extensions.Select(e => e.TrimStart('.')), StringComparer.OrdinalIgnoreCase);
        _debounce = new Timer(_ => Changed?.Invoke(), null, Timeout.Infinite, Timeout.Infinite);
        _watcher = new FileSystemWatcher(root)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size,
            InternalBufferSize = 64 * 1024,
        };
        _watcher.Created += OnEvent;
        _watcher.Deleted += OnEvent;
        _watcher.Changed += OnEvent;
        _watcher.Renamed += (s, e) => { OnEvent(s, e); Touch(e.OldFullPath); };
        _watcher.Error += (_, _) => Kick(); // buffer overflow: rescan to be safe
        _watcher.EnableRaisingEvents = true;

        var interval = pollInterval ?? (IsNetworkPath(root) ? TimeSpan.FromMinutes(2) : TimeSpan.Zero);
        if (interval > TimeSpan.Zero)
        {
            _lastSnapshot = Snapshot();
            _poll = new Timer(_ => Poll(), null, interval, interval);
        }
    }

    public static bool IsNetworkPath(string path)
    {
        try
        {
            var full = Path.GetFullPath(path);
            if (full.StartsWith(@"\\", StringComparison.Ordinal)) return true;
            var root = Path.GetPathRoot(full);
            return !string.IsNullOrEmpty(root) && new DriveInfo(root).DriveType == DriveType.Network;
        }
        catch { return false; }
    }

    private (int Count, long Size, long Newest)? Snapshot()
    {
        try
        {
            int count = 0;
            long size = 0, newest = 0;
            foreach (var file in new DirectoryInfo(Root).EnumerateFiles("*", SearchOption.AllDirectories))
            {
                if (!_extensions.Contains(file.Extension.TrimStart('.'))) continue;
                count++;
                size += file.Length;
                newest = Math.Max(newest, file.LastWriteTimeUtc.Ticks);
            }
            return (count, size, newest);
        }
        catch { return null; } // share offline: try again next time
    }

    private void Poll()
    {
        var now = Snapshot();
        if (now is null || now == _lastSnapshot) return;
        var changed = _lastSnapshot is not null;
        _lastSnapshot = now;
        if (changed) Kick();
    }

    private void OnEvent(object sender, FileSystemEventArgs e) => Touch(e.FullPath);

    private void Touch(string path)
    {
        // Folder events (e.g. a whole game folder moved) have no extension; treat them as relevant.
        var ext = Path.GetExtension(path).TrimStart('.');
        if (ext.Length == 0 || _extensions.Contains(ext))
            Kick();
    }

    private void Kick() => _debounce.Change(_delay, Timeout.InfiniteTimeSpan);

    public void Dispose()
    {
        _watcher.Dispose();
        _poll?.Dispose();
        _debounce.Dispose();
    }
}
