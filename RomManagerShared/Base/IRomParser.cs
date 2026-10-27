using RomManagerShared.Interfaces;

namespace RomManagerShared.Base;

public interface IRomParser<T> where T : GamingConsole
{
    List<string> Extensions { get; set; }

    Task<List<Rom>> ProcessFile(string path);

    /// <summary>
    /// Cancellable parse. Parsers doing long reads (compressed or large disc images) override this; the default
    /// calls <see cref="ProcessFile(string)"/>, and the executor still stops between parsers when cancelled.
    /// </summary>
    Task<List<Rom>> ProcessFile(string path, CancellationToken ct) => ProcessFile(path);
}

/// <summary>
/// Optional for parsers: rules a file out from its first bytes, before the parser opens it. Lets formats
/// that share an extension (<c>.iso</c>, <c>.pkg</c>) go to the right parser and console, and gives a clear
/// "not recognised" reason.
/// </summary>
public interface IContentSniffer
{
    /// <summary>False when <paramref name="header"/> (the first <see cref="FileMagic.HeaderLength"/> bytes,
    /// fewer for short files) is certainly not this parser's format.</summary>
    bool CanParse(string path, ReadOnlySpan<byte> header);
}

/// <summary>What a file parsed to, with what the parsers logged; <see cref="UnidentifiedReason"/> is set when empty.</summary>
public sealed record ParseResult(List<Rom> Roms, IReadOnlyList<string> Diagnostics)
{
    public string? UnidentifiedReason => Roms.Count == 0 ? Utils.ParseDiagnostics.Summarize(Diagnostics) : null;
}
