using System.Globalization;
using System.Text;
using System.Text.Json;
using RomManagerShared.Base;

namespace RomManagerShared.Utils;

/// <summary>One exported library row.</summary>
public sealed record ExportRow(
    string Console, string Type, string TitleId, string Title, string? Version, string Regions, string Languages,
    string Format, long? Size, string Path, string? Sha1, bool Verified)
{
    public static ExportRow From(string console, Rom rom)
    {
        var sha1 = rom.Hashes?.FirstOrDefault(h => h.Type == HashTypeEnum.SHA1);
        return new ExportRow(
            console,
            rom switch { Update => "Update", DLC => "DLC", Homebrew => "Homebrew", _ => "Game" },
            rom.TitleID ?? "",
            rom.Titles?.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Value))?.Value ?? "",
            rom.Version,
            string.Join("; ", rom.Regions ?? []),
            string.Join("; ", (rom.Languages ?? []).Distinct()),
            System.IO.Path.GetExtension(rom.Path ?? "").TrimStart('.').ToUpperInvariant(),
            rom.Size,
            rom.Path ?? "",
            sha1?.Value,
            sha1?.IsVerified ?? false);
    }
}

/// <summary>Writes library rows as CSV (RFC 4180, UTF-8) or JSON.</summary>
public static class LibraryExporter
{
    private static readonly string[] Header =
        ["Console", "Type", "TitleId", "Title", "Version", "Regions", "Languages", "Format", "Size", "Path", "Sha1", "Verified"];

    public static void WriteCsv(IEnumerable<ExportRow> rows, TextWriter writer)
    {
        writer.WriteLine(string.Join(",", Header));
        foreach (var r in rows)
        {
            string?[] fields =
            [
                r.Console, r.Type, r.TitleId, r.Title, r.Version, r.Regions, r.Languages, r.Format,
                r.Size?.ToString(CultureInfo.InvariantCulture), r.Path, r.Sha1, r.Verified ? "true" : "false",
            ];
            writer.WriteLine(string.Join(",", fields.Select(Escape)));
        }
    }

    public static void WriteJson(IEnumerable<ExportRow> rows, Stream stream) =>
        JsonSerializer.Serialize(stream, rows.ToList(), new JsonSerializerOptions { WriteIndented = true });

    /// <summary>Picks CSV or JSON from the file extension.</summary>
    public static void Export(IEnumerable<ExportRow> rows, string path)
    {
        if (System.IO.Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            using var stream = File.Create(path);
            WriteJson(rows, stream);
            return;
        }
        using var writer = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        WriteCsv(rows, writer);
    }

    // Quote fields containing separators, quotes or line breaks; neutralise spreadsheet formulas.
    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value[0] is '=' or '+' or '-' or '@') value = "'" + value;
        return value.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }
}
