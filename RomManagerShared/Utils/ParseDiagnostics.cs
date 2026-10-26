namespace RomManagerShared.Utils;

/// <summary>
/// Collects the <see cref="FileUtils.Log"/> messages written while one file is parsed, so the scanner
/// can say why a file produced no roms. Parsers keep logging as before; nothing is threaded through them.
/// </summary>
public static class ParseDiagnostics
{
    private static readonly AsyncLocal<List<string>?> Current = new();

    /// <summary>Runs <paramref name="parse"/> and returns its result with the messages it logged.</summary>
    public static async Task<(T Result, IReadOnlyList<string> Messages)> CaptureAsync<T>(Func<Task<T>> parse)
    {
        var messages = new List<string>();
        Current.Value = messages;
        try
        {
            return (await parse(), messages);
        }
        finally
        {
            Current.Value = null;
        }
    }

    internal static void Add(string message)
    {
        if (Current.Value is not { } list) return;
        lock (list) list.Add(message);
    }

    /// <summary>A short reason for a file that parsed to nothing.</summary>
    public static string Summarize(IReadOnlyList<string> messages)
    {
        var text = messages.Count == 0
            ? "No parser recognised the file"
            : string.Join(" | ", messages.Select(m => m.Trim()).Where(m => m.Length > 0).Distinct());
        return text.Length <= 500 ? text : text[..497] + "...";
    }
}
