using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using AvaloniaUI.Resources;
using AwesomeAssertions;

namespace RomManagerTests;

public class LocalizationTests
{
    private static IEnumerable<(string Key, string Value)> AllStrings() =>
        Strings.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)!
            .Cast<DictionaryEntry>().Select(e => ((string)e.Key, (string)e.Value!));

    [Fact]
    public void EveryStringHasText() =>
        AllStrings().Where(s => string.IsNullOrWhiteSpace(s.Value)).Should().BeEmpty();

    // A broken placeholder ("{0" or "{0:N0") would throw when the UI formats it. Strings without numbered
    // placeholders are shown as they are ("{rom}" in the emulator hint is literal text).
    [Fact]
    public void EveryFormatStringFormats()
    {
        var args = Enumerable.Repeat<object>(new AnyFormat(), 10).ToArray();
        foreach (var (key, value) in AllStrings().Where(s => Regex.IsMatch(s.Value, @"\{\d")))
        {
            var act = () => string.Format(CultureInfo.InvariantCulture, value, args);
            act.Should().NotThrow(key);
        }
    }

    [Fact]
    public void Labels_UseResourcesAndFallBackToTheKey()
    {
        Labels.Property("TitleID").Should().Be("Title ID");
        Labels.Property("Release Date").Should().Be("Release date");
        Labels.Property("SomeModuleProperty").Should().Be("SomeModuleProperty");
        Labels.Property("SomeModuleProperty", "Module header").Should().Be("Module header");
        Labels.Value("HasUpdate").Should().Be("Has update");
        Labels.Value("Europe").Should().Be("Europe");
    }

    private sealed class AnyFormat : IFormattable
    {
        public string ToString(string? format, IFormatProvider? provider) => "x";
    }
}
