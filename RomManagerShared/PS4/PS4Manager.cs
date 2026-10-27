using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.PS4.Parsers;
using RomManagerShared.Utils;
namespace RomManagerShared.PS4;

public class PS4Manager(RomParserExecutor<PS4Console> romParserExecutor)
    : ConsoleManager<PS4Console>(romParserExecutor), IMissingUpdateReporter
{
    // ── IMissingUpdateReporter: Sony's update server, opt-in ──────────────
    public bool HasVersionDatabase => PS4UpdateChecker.OnlineAllowed;

    public string SetupPrompt =>
        "Checking PS4 updates asks Sony's update server about each game in the library (one request per game). Allow it for this session? (Settings can allow it permanently.)";

    public bool IsOnline => true;

    public Task DownloadVersionDatabaseAsync()
    {
        PS4UpdateChecker.OnlineAllowed = true;
        return Task.CompletedTask;
    }

    public IReadOnlyList<MissingUpdate> FindMissingUpdates(IReadOnlyList<Rom> library) =>
        FindMissingUpdatesAsync(library).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<MissingUpdate>> FindMissingUpdatesAsync(IReadOnlyList<Rom> library, CancellationToken ct = default)
    {
        if (!PS4UpdateChecker.OnlineAllowed) return [];
        using var http = PS4UpdateChecker.CreateClient();
        return await PS4UpdateChecker.FindMissingAsync(library, (url, token) => PS4UpdateChecker.FetchAsync(http, url, token), ct);
    }
}
