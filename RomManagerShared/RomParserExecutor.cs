using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
namespace RomManagerShared;

/// <summary>The console-independent face of <see cref="RomParserExecutor{T}"/>.</summary>
public interface IRomParserExecutor
{
    List<string> GetSupportedExtensions();

    /// <summary>Parses a file and returns the roms with what the parsers logged.</summary>
    Task<ParseResult> ParseAsync(string file, CancellationToken ct = default);

    /// <summary>False when every parser for the file's extension rules the header out.</summary>
    bool MightParse(string path, ReadOnlySpan<byte> header);
}

public class RomParserExecutor<T> : IRomParserExecutor where T : GamingConsole
{
    public List<IRomParser<T>> Parsers = [];

    public RomParserExecutor(IEnumerable<IRomParser<T>> titleparsers)
    {
        Parsers = [.. titleparsers.OrderByDescending(p => p.Extensions.Count)];
    }

    public void AddParser(IRomParser<T> parser)
    {
        Parsers.Add(parser);
        Parsers = [.. Parsers.OrderByDescending(p => p.Extensions.Count)];
    }
    public async Task<List<Rom>> ExecuteParsers(string file) => await ExecuteParsers(file, CancellationToken.None);

    /// <summary>Parses <paramref name="file"/> and returns the roms with what the parsers logged.</summary>
    public async Task<ParseResult> ParseAsync(string file, CancellationToken ct = default)
    {
        var (roms, messages) = await ParseDiagnostics.CaptureAsync(() => ExecuteParsers(file, ct));
        return new ParseResult(roms, messages);
    }

    public async Task<List<Rom>> ExecuteParsers(string file, CancellationToken ct)
    {
        List<Rom> mergedRomList = [];
        var extension = Path.GetExtension(file).TrimStart('.').ToLowerInvariant();
        var compatibleParsers = Parsers.Where(x => x.Extensions.Contains(extension)).ToList();

        // Read the header once, only when a parser can rule the file out from it.
        byte[]? header = null;
        if (compatibleParsers.Any(p => p is IContentSniffer) && File.Exists(file))
        {
            try { header = FileMagic.ReadHeader(file); }
            catch (Exception ex) { AppLog.Warn($"file '{file}': cannot read header: {ex.Message}"); }
        }

        foreach (var parser in compatibleParsers)
        {
            ct.ThrowIfCancellationRequested();
            if (header is not null && parser is IContentSniffer sniffer && !sniffer.CanParse(file, header))
            {
                AppLog.Info($"{Path.GetFileName(file)}: not a {ConsoleModules.IdOf(typeof(T))} file ({parser.GetType().Name})");
                continue;
            }

            List<Rom>? parsedRomList;
            try
            {
                parsedRomList = await parser.ProcessFile(file, ct);
                parsedRomList?.RemoveAll(IsEmpty);
                if (parsedRomList == null || parsedRomList.Count == 0)
                {
                    continue;
                }

                // Merge parsed ROMs into already-accumulated results by TitleID.
                // Build the remove-list first to avoid modifying the collection during iteration.
                var toMerge = new List<Rom>();
                foreach (var parsedrom in parsedRomList)
                {
                    var existing = mergedRomList.FirstOrDefault(m =>
                        parsedrom.TitleID != null && parsedrom.TitleID == m.TitleID);
                    if (existing != null)
                    {
                        existing.MergeFrom(parsedrom);
                        toMerge.Add(parsedrom);
                    }
                }
                foreach (var r in toMerge) parsedRomList.Remove(r);
                mergedRomList.AddRange(parsedRomList);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                AppLog.Warn($"file '{file}' threw exception {ex.Message}.{Environment.NewLine}");
            }
        }
        return mergedRomList;
    }

    // A parser that matched the extension but found nothing usable.
    private static bool IsEmpty(Rom rom) =>
        string.IsNullOrWhiteSpace(rom.TitleID) && string.IsNullOrWhiteSpace(rom.ProductCode)
        && (rom.Titles is null || rom.Titles.Count == 0);

    public bool MightParse(string path, ReadOnlySpan<byte> header)
    {
        var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        foreach (var parser in Parsers)
            if (parser.Extensions.Contains(extension) && (parser is not IContentSniffer sniffer || sniffer.CanParse(path, header)))
                return true;
        return false;
    }

    /// <summary>Every extension a parser accepts, once.</summary>
    public List<string> GetSupportedExtensions() =>
        [.. Parsers.SelectMany(p => p.Extensions).Distinct(StringComparer.OrdinalIgnoreCase)];

}
