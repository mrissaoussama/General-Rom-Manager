using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using AvaloniaUI.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaUI.ViewModels;

// ---------------------------------------------------------------------------
// Single download entry observable from the UI
// ---------------------------------------------------------------------------
public partial class DownloadEntryViewModel : ObservableObject
{
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    private double _progressPercent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    private long _bytesReceived;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    private long _totalBytes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeedText))]
    private double _speedKbps;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPause))]
    [NotifyPropertyChangedFor(nameof(CanResume))]
    [NotifyPropertyChangedFor(nameof(CanRetry))]
    private DownloadStatus _status = DownloadStatus.Queued;
    [ObservableProperty] private string _errorMessage = string.Empty;

    // ── Pause support ─────────────────────────────────────────────────────
    private volatile bool _pauseRequested;
    public bool IsPauseRequested => _pauseRequested;

    // ── Retry support ─────────────────────────────────────────────────────
    /// <summary>Set by DownloadManagerViewModel so the entry can restart itself.</summary>
    public Func<Task>? RetryCallback { get; set; }
    public string? SourceUrl { get; set; }
    public string? DestinationPath { get; set; }

    public CancellationTokenSource Cts { get; private set; } = new();

    /// <summary>Human-readable progress, e.g. "12.3 MB / 45.0 MB".</summary>
    public string ProgressText =>
        TotalBytes > 0
            ? $"{FormatBytes(BytesReceived)} / {FormatBytes(TotalBytes)}"
            : FormatBytes(BytesReceived);

    /// <summary>Human-readable speed, e.g. "1.5 MB/s".</summary>
    public string SpeedText =>
        SpeedKbps >= 1024
            ? $"{SpeedKbps / 1024.0:F1} MB/s"
            : $"{SpeedKbps:F0} KB/s";

    /// <summary>Whether the Pause button should be visible.</summary>
    public bool CanPause => Status is DownloadStatus.Downloading or DownloadStatus.Queued;
    /// <summary>Whether the Resume button should be visible.</summary>
    public bool CanResume => Status == DownloadStatus.Paused;
    /// <summary>Whether the Retry button should be visible.</summary>
    public bool CanRetry => Status is DownloadStatus.Failed or DownloadStatus.Cancelled;

    private static string FormatBytes(long bytes) =>
        bytes switch
        {
            >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
            >= 1_048_576     => $"{bytes / 1_048_576.0:F1} MB",
            >= 1_024         => $"{bytes / 1_024.0:F1} KB",
            _                => $"{bytes} B",
        };

    [RelayCommand]
    public void Cancel() => Cts.Cancel();

    [RelayCommand]
    public void Pause()
    {
        if (Status is not (DownloadStatus.Downloading or DownloadStatus.Queued)) return;
        _pauseRequested = true;
        Status = DownloadStatus.Paused;
        OnPropertyChanged(nameof(CanPause));
        OnPropertyChanged(nameof(CanResume));
    }

    [RelayCommand]
    public void Resume()
    {
        if (Status != DownloadStatus.Paused) return;
        _pauseRequested = false;
        Status = DownloadStatus.Downloading;
        OnPropertyChanged(nameof(CanPause));
        OnPropertyChanged(nameof(CanResume));
    }

    [RelayCommand]
    public async Task Retry()
    {
        if (RetryCallback is null || Status is not (DownloadStatus.Failed or DownloadStatus.Cancelled)) return;
        // Reset state
        Cts = new CancellationTokenSource();
        _pauseRequested = false;
        BytesReceived  = 0;
        ProgressPercent = 0;
        SpeedKbps = 0;
        ErrorMessage = string.Empty;
        Status = DownloadStatus.Queued;
        OnPropertyChanged(nameof(CanPause));
        OnPropertyChanged(nameof(CanResume));
        OnPropertyChanged(nameof(CanRetry));
        await RetryCallback();
    }
}

public enum DownloadStatus
{
    Queued,
    Downloading,
    Paused,
    Completed,
    Failed,
    Cancelled,
}

// ---------------------------------------------------------------------------
// Aggregate view-model used as the DataContext of the download panel
// ---------------------------------------------------------------------------
public partial class DownloadManagerViewModel : ObservableObject
{
    public ObservableCollection<DownloadEntryViewModel> Downloads { get; } = new();

    [ObservableProperty] private bool _isPanelExpanded = Settings.Instance.IsDownloadPanelExpanded;

    partial void OnIsPanelExpandedChanged(bool value)
    {
        Settings.Instance.IsDownloadPanelExpanded = value;
        Settings.Instance.Save();
    }

    /// <summary>Total count of entries that are still in progress.</summary>
    public int ActiveCount =>
        Downloads.Count(d => d.Status is DownloadStatus.Downloading or DownloadStatus.Queued);

    /// <summary>
    /// Adds a new entry and returns it so the caller can update progress.
    /// </summary>
    public DownloadEntryViewModel Enqueue(string fileName, long totalBytes = 0)
    {
        var entry = new DownloadEntryViewModel
        {
            FileName = fileName,
            TotalBytes = totalBytes,
            Status = DownloadStatus.Queued,
        };
        Downloads.Add(entry);
        OnPropertyChanged(nameof(ActiveCount));
        return entry;
    }

    [RelayCommand]
    private void ClearCompleted()
    {
        var done = Downloads
            .Where(d => d.Status is DownloadStatus.Completed
                                 or DownloadStatus.Failed
                                 or DownloadStatus.Cancelled)
            .ToList();

        foreach (var d in done) Downloads.Remove(d);
        OnPropertyChanged(nameof(ActiveCount));
    }

    [RelayCommand]
    private void PauseAll()
    {
        foreach (var d in Downloads
            .Where(d => d.Status is DownloadStatus.Downloading or DownloadStatus.Queued))
            d.PauseCommand.Execute(null);
    }

    [RelayCommand]
    private void ResumeAll()
    {
        foreach (var d in Downloads.Where(d => d.Status == DownloadStatus.Paused))
            d.ResumeCommand.Execute(null);
    }

    [RelayCommand]
    private void TogglePanel() => IsPanelExpanded = !IsPanelExpanded;

    // ── HTTP download ─────────────────────────────────────────────────────

    private static readonly HttpClient _http;
    static DownloadManagerViewModel()
    {
        var handler = new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All };
        _http = new HttpClient(handler) { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("RomManager/1.0");
        _http.DefaultRequestHeaders.Accept.ParseAdd("*/*");
    }

    /// <summary>
    /// Enqueues and immediately starts downloading <paramref name="url"/> to
    /// <paramref name="destinationPath"/>.  Progress is reflected on the
    /// returned <see cref="DownloadEntryViewModel"/>.
    /// </summary>
    /// <summary>
    /// Returns true if a download for the given destination is already queued or in progress.
    /// </summary>
    public bool IsAlreadyQueued(string destinationPath)
        => Downloads.Any(d => string.Equals(d.DestinationPath, destinationPath, StringComparison.OrdinalIgnoreCase)
                           && d.Status is DownloadStatus.Queued or DownloadStatus.Downloading or DownloadStatus.Paused);

    public async Task<DownloadEntryViewModel> DownloadAsync(
        string url,
        string destinationPath,
        string? displayName = null)
    {
        // Prevent duplicate downloads to the same destination
        if (IsAlreadyQueued(destinationPath))
            return Downloads.First(d => string.Equals(d.DestinationPath, destinationPath, StringComparison.OrdinalIgnoreCase));

        string fileName;
        try { fileName = displayName ?? Path.GetFileName(new Uri(url).LocalPath); }
        catch { fileName = displayName ?? Path.GetFileName(destinationPath); }

        var entry = Enqueue(fileName);
        entry.SourceUrl = url;
        entry.DestinationPath = destinationPath;
        // Allow the entry to restart itself
        entry.RetryCallback = () => RequeueDownloadAsync(entry);

        _ = Task.Run(() => RunDownloadAsync(entry, url, destinationPath));
        return entry;
    }

    private Task RequeueDownloadAsync(DownloadEntryViewModel entry)
    {
        if (entry.SourceUrl is null || entry.DestinationPath is null) return Task.CompletedTask;
        _ = Task.Run(() => RunDownloadAsync(entry, entry.SourceUrl, entry.DestinationPath));
        return Task.CompletedTask;
    }

    private async Task RunDownloadAsync(DownloadEntryViewModel entry, string url, string destPath)
    {
        var ct = entry.Cts.Token;
        try
        {
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(destPath))
                throw new ArgumentException("URL or destination path is empty.");

            entry.Status = DownloadStatus.Downloading;

            using var response = await _http.GetAsync(url,
                HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0L;

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                entry.TotalBytes = totalBytes;
            });

            var dir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var file   = File.Create(destPath);

            var buffer    = new byte[81_920]; // 80 KB chunks
            long received = 0L;
            var sw        = Stopwatch.StartNew();
            long lastBytes = 0L;
            var lastTick   = sw.Elapsed;
            int read;

            while ((read = await stream.ReadAsync(buffer, ct)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read), ct);
                received += read;

                // Handle pause: spin until resumed or cancelled
                while (entry.IsPauseRequested && !ct.IsCancellationRequested)
                    await Task.Delay(200, ct);
                if (ct.IsCancellationRequested) break;

                // Throttle UI updates to ~10 Hz
                var now = sw.Elapsed;
                if ((now - lastTick).TotalMilliseconds >= 100)
                {
                    var deltaSec  = (now - lastTick).TotalSeconds;
                    var deltaBytes = received - lastBytes;
                    var kbps       = deltaSec > 0 ? deltaBytes / deltaSec / 1024.0 : 0;
                    lastTick  = now;
                    lastBytes = received;

                    var snap = (received, totalBytes, kbps);
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        entry.BytesReceived   = snap.received;
                        entry.ProgressPercent = snap.totalBytes > 0
                            ? snap.received * 100.0 / snap.totalBytes
                            : 0;
                        entry.SpeedKbps = snap.kbps;
                        // ProgressText/SpeedText update via [NotifyPropertyChangedFor]
                    });
                }
            }

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                entry.BytesReceived    = received;
                entry.ProgressPercent  = 100;
                entry.Status           = DownloadStatus.Completed;
                OnPropertyChanged(nameof(ActiveCount));
            });
        }
        catch (OperationCanceledException)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                entry.Status = DownloadStatus.Cancelled;
                OnPropertyChanged(nameof(ActiveCount));
            });
        }
        catch (Exception ex)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                entry.ErrorMessage = ex.Message;
                entry.Status       = DownloadStatus.Failed;
                OnPropertyChanged(nameof(ActiveCount));
            });
        }
    }
}
