namespace RomManagerShared.Base;

/// <summary>A base game whose newest installed version is older than the newest released one.</summary>
public sealed record MissingUpdate(Rom Game, string InstalledVersion, string LatestVersion)
{
    public string Title => Game.Titles?.FirstOrDefault()?.Value ?? Game.TitleID ?? "";
}

/// <summary>
/// Implemented by console managers that can tell which games have newer updates than the ones in
/// the library. The UI shows a "Missing updates" report for these consoles.
/// </summary>
public interface IMissingUpdateReporter
{
    /// <summary>True when the local version database exists.</summary>
    bool HasVersionDatabase { get; }

    Task DownloadVersionDatabaseAsync();

    /// <summary>Checks the whole library (games and update files), not just a selection.</summary>
    IReadOnlyList<MissingUpdate> FindMissingUpdates(IReadOnlyList<Rom> library);
}
