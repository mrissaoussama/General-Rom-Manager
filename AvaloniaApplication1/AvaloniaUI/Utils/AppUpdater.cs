using Velopack;
using Velopack.Sources;

namespace AvaloniaUI.Utils;

/// <summary>Updates of RomManager itself, from the project's GitHub releases (Velopack).</summary>
public interface IAppUpdater
{
    /// <summary>False when running from a build folder or a zip rather than an installation: nothing to update.</summary>
    bool IsInstalled { get; }

    /// <summary>The newer version, or null when up to date.</summary>
    Task<string?> CheckAsync(CancellationToken ct = default);

    /// <summary>Downloads the update found by <see cref="CheckAsync"/>, then restarts into it.</summary>
    Task DownloadAndRestartAsync(IProgress<int>? progress = null);
}

public sealed class VelopackUpdater : IAppUpdater
{
    public const string ReleasesUrl = "https://github.com/mrissaoussama/General-Rom-Manager";

    private readonly UpdateManager _manager = new(new GithubSource(ReleasesUrl, accessToken: null, prerelease: false));
    private UpdateInfo? _pending;

    public bool IsInstalled => _manager.IsInstalled;

    public async Task<string?> CheckAsync(CancellationToken ct = default)
    {
        if (!IsInstalled) return null;
        _pending = await _manager.CheckForUpdatesAsync();
        return _pending?.TargetFullRelease.Version.ToString();
    }

    public async Task DownloadAndRestartAsync(IProgress<int>? progress = null)
    {
        if (_pending is null) return;
        await _manager.DownloadUpdatesAsync(_pending, p => progress?.Report(p));
        _manager.ApplyUpdatesAndRestart(_pending);
    }
}
