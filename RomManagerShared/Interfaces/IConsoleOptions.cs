using RomManagerShared.Base;

namespace RomManagerShared.Interfaces;

/// <summary>A line of console status shown in Settings (e.g. "prod.keys: Found, 42 keys"); Ok null = neutral.</summary>
public sealed record ConsoleStatusLine(string Text, bool? Ok);

/// <summary>
/// Options and status a console module shows in Settings → Consoles, found automatically like parsers. Values are
/// stored by the app (settings.json) and handed back through <see cref="Apply"/> at startup and after each change.
/// </summary>
public interface IConsoleOptions
{
    /// <summary>Heading in Settings, e.g. "Nintendo Switch".</summary>
    string Title { get; }

    IReadOnlyList<PluginSettingDescriptor> Options { get; }

    /// <summary>Takes the stored values (defaults where unset) and applies them.</summary>
    void Apply(IReadOnlyDictionary<string, string> values);

    /// <summary>Things worth showing next to the options (keys found, tools present); never secrets.</summary>
    Task<IReadOnlyList<ConsoleStatusLine>> GetStatusAsync() => Task.FromResult<IReadOnlyList<ConsoleStatusLine>>([]);
}
