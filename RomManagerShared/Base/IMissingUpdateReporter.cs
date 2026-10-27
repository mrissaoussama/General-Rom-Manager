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
    /// <summary>True when the check can run (the local version database exists, or the online check is allowed).</summary>
    bool HasVersionDatabase { get; }

    /// <summary>What to ask the user before <see cref="DownloadVersionDatabaseAsync"/>.</summary>
    string SetupPrompt => "The version database has not been downloaded yet. Download it now?";

    /// <summary>Makes <see cref="HasVersionDatabase"/> true (downloads the database, or allows the online check).</summary>
    Task DownloadVersionDatabaseAsync();

    /// <summary>True when every check goes over the network; the dashboard does not count these on its own.</summary>
    bool IsOnline => false;

    /// <summary>Checks the whole library (games and update files), not just a selection.</summary>
    IReadOnlyList<MissingUpdate> FindMissingUpdates(IReadOnlyList<Rom> library);

    Task<IReadOnlyList<MissingUpdate>> FindMissingUpdatesAsync(IReadOnlyList<Rom> library, CancellationToken ct = default) =>
        Task.Run(() => FindMissingUpdates(library), ct);
}

/// <summary>
/// Implemented by console managers that know which DLC exist for a game. The UI shows a "Missing DLC" report.
/// Each line is "game title: DLC name [DLC title ID]".
/// </summary>
public interface IMissingDlcReporter
{
    /// <summary>Loads the title database if needed and lists DLC of library games that the library lacks.</summary>
    Task<IReadOnlyList<(string Game, string Dlc, string TitleId)>> FindMissingDlcAsync(IReadOnlyList<Rom> library);
}
