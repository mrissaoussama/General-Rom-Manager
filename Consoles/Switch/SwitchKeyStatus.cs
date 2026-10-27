using System.Globalization;

namespace RomManagerShared.Switch;

/// <summary>Metadata about one key file. Never holds key values.</summary>
public sealed record KeyFileSummary(string Path, bool Exists, int KeyCount, string? Error = null);

/// <summary>
/// Summarises prod.keys / title.keys (existence, counts, highest master key) without
/// exposing any key material.
/// </summary>
public sealed class SwitchKeyStatus
{
    public required KeyFileSummary ProdKeys { get; init; }
    public required KeyFileSummary TitleKeys { get; init; }

    /// <summary>Highest <c>master_key_XX</c> revision in prod.keys, or null if none.</summary>
    public int? HighestMasterKey { get; init; }

    /// <summary>Approximate minimum firmware the highest master key covers, or null if unknown.</summary>
    public string? FirmwareHint => HighestMasterKey is { } rev && rev < FirmwareByMasterKey.Length
        ? FirmwareByMasterKey[rev]
        : null;

    // Index = master key revision.
    private static readonly string[] FirmwareByMasterKey =
    [
        "1.0.0", "3.0.0", "3.0.1", "4.0.0", "5.0.0", "6.0.0", "6.2.0", "7.0.0",
        "8.1.0", "9.0.0", "9.1.0", "12.1.0", "13.0.0", "14.0.0", "15.0.0", "16.0.0",
        "17.0.0", "18.0.0", "19.0.0", "20.0.0",
    ];

    public static string DefaultKeysDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".switch");

    public static SwitchKeyStatus ReadDefault() => Read(
        Path.Combine(DefaultKeysDirectory, "prod.keys"),
        Path.Combine(DefaultKeysDirectory, "title.keys"));

    public static SwitchKeyStatus Read(string prodKeysPath, string titleKeysPath)
    {
        int? highest = null;
        var prod = Summarise(prodKeysPath, name =>
        {
            if (TryParseMasterKeyRevision(name, out var rev) && (highest is null || rev > highest))
                highest = rev;
        });
        var title = Summarise(titleKeysPath, null);
        return new SwitchKeyStatus { ProdKeys = prod, TitleKeys = title, HighestMasterKey = highest };
    }

    // Counts "name = hex" lines; only the name is passed on, the value is discarded.
    private static KeyFileSummary Summarise(string path, Action<string>? onKeyName)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new KeyFileSummary(path, false, 0);
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(fs);
            int count = 0;
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                var eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var name = line[..eq].Trim();
                var value = line[(eq + 1)..].Trim();
                if (name.Length == 0 || name.StartsWith(';') || name.StartsWith('#')) continue;
                if (value.Length == 0 || !IsHex(value)) continue;
                count++;
                onKeyName?.Invoke(name);
            }
            return new KeyFileSummary(path, true, count);
        }
        catch (Exception ex)
        {
            return new KeyFileSummary(path, true, 0, ex.GetType().Name);
        }
    }

    private static bool TryParseMasterKeyRevision(string name, out int rev)
    {
        rev = 0;
        const string prefix = "master_key_";
        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        var suffix = name[prefix.Length..];
        return suffix.Length == 2 &&
               int.TryParse(suffix, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rev);
    }

    private static bool IsHex(string s)
    {
        foreach (var c in s)
            if (!char.IsAsciiHexDigit(c)) return false;
        return true;
    }
}
