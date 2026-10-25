namespace RomManagerShared.Base.Library;

/// <summary>
/// Watches a ROM folder and raises <see cref="Changed"/> once activity settles. Network shares can drop
/// events, so this only speeds things up; the incremental scan stays the source of truth.
/// </summary>
public sealed class LibraryWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly HashSet<string> _extensions;
    private readonly Timer _debounce;
    private readonly TimeSpan _delay;

    public string Root { get; }
    public event Action? Changed;

    public LibraryWatcher(string root, IEnumerable<string> extensions, TimeSpan? delay = null)
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
        _debounce.Dispose();
    }
}
