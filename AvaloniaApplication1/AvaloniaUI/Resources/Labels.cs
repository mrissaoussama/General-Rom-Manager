using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia.Data.Converters;

namespace AvaloniaUI.Resources;

/// <summary>
/// Text for values that are also keys: rom property names ("TitleID", "Release Date"), rom types and filter
/// choices ("Game", "Has update"). The key stays the same in code and settings; the label comes from
/// <see cref="Strings"/> (<c>Property_*</c>, <c>Value_*</c>) and falls back to the key itself, so data values
/// such as region or language names show as they are.
/// </summary>
public static partial class Labels
{
    [GeneratedRegex("[^A-Za-z0-9]")] private static partial Regex NonWord();

    private static string? Lookup(string prefix, string key) =>
        Strings.ResourceManager.GetString(prefix + NonWord().Replace(key, ""), Strings.Culture);

    /// <summary>Label of a rom property (grid column, details, properties dialog).</summary>
    public static string Property(string key, string? fallback = null) => Lookup("Property_", key) ?? fallback ?? key;

    /// <summary>Label of a rom type or filter choice.</summary>
    public static string Value(string key) => Lookup("Value_", key) ?? key;
}

/// <summary>Shows a key through <see cref="Labels.Value"/> (filter combo boxes).</summary>
public sealed class ValueLabelConverter : IValueConverter
{
    public static readonly ValueLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s ? Labels.Value(s) : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
