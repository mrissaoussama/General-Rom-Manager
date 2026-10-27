namespace RomManagerShared.Utils;

/// <summary>
/// Orders version strings by their numeric parts: "v1.10" &gt; "1.9", "2.0" &gt; "1.10", "01.02" = "1.2",
/// "65536" (Switch) &gt; "0". Any non-digit separates parts; text without digits is version 0. Independent of
/// the current culture.
/// </summary>
public readonly struct VersionKey : IComparable<VersionKey>, IEquatable<VersionKey>
{
    private readonly ulong[]? _parts;

    private VersionKey(ulong[] parts) => _parts = parts;

    public static VersionKey Parse(string? version)
    {
        if (string.IsNullOrEmpty(version)) return default;
        var parts = new List<ulong>();
        ulong current = 0;
        bool inNumber = false;
        foreach (var c in version)
        {
            if (c is >= '0' and <= '9')
            {
                current = current > (ulong.MaxValue - 9) / 10 ? ulong.MaxValue : current * 10 + (ulong)(c - '0');
                inNumber = true;
            }
            else if (inNumber)
            {
                parts.Add(current);
                current = 0;
                inNumber = false;
            }
        }
        if (inNumber) parts.Add(current);
        // Trailing zeros do not change the version: "1.2" == "1.2.0".
        while (parts.Count > 0 && parts[^1] == 0) parts.RemoveAt(parts.Count - 1);
        return new VersionKey([.. parts]);
    }

    public int CompareTo(VersionKey other)
    {
        var a = _parts ?? [];
        var b = other._parts ?? [];
        for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            var x = i < a.Length ? a[i] : 0;
            var y = i < b.Length ? b[i] : 0;
            if (x != y) return x.CompareTo(y);
        }
        return 0;
    }

    public static int Compare(string? a, string? b) => Parse(a).CompareTo(Parse(b));

    public bool Equals(VersionKey other) => CompareTo(other) == 0;
    public override bool Equals(object? obj) => obj is VersionKey other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var part in _parts ?? []) hash.Add(part);
        return hash.ToHashCode();
    }

    public static bool operator ==(VersionKey a, VersionKey b) => a.Equals(b);
    public static bool operator !=(VersionKey a, VersionKey b) => !a.Equals(b);
    public static bool operator <(VersionKey a, VersionKey b) => a.CompareTo(b) < 0;
    public static bool operator >(VersionKey a, VersionKey b) => a.CompareTo(b) > 0;
    public static bool operator <=(VersionKey a, VersionKey b) => a.CompareTo(b) <= 0;
    public static bool operator >=(VersionKey a, VersionKey b) => a.CompareTo(b) >= 0;

    public override string ToString() => string.Join('.', _parts ?? []);
}
