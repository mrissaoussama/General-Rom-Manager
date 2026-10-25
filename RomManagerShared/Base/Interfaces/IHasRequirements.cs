namespace RomManagerShared.Base;

/// <summary>What kind of external resource a requirement points to.</summary>
public enum RequirementKind
{
    /// <summary>A file that must exist on disk (e.g. prod.keys).</summary>
    FilePath,
    /// <summary>A directory that must exist (e.g. titledb folder).</summary>
    DirectoryPath,
    /// <summary>A URL that must be reachable (e.g. titledb download).</summary>
    Url,
    /// <summary>A cryptographic key file.</summary>
    KeyFile,
    /// <summary>A database / JSON resource file.</summary>
    DatabaseFile,
    /// <summary>A generic configuration value.</summary>
    Configuration,
}

/// <summary>
/// Describes a single external dependency that a console, parser, or plugin
/// needs in order to function.  Requirements are portable — the same
/// requirement definition can be shared between parsers, console managers,
/// and even plugins.
/// </summary>
public sealed record ConsoleRequirement
{
    /// <summary>Machine-readable unique key, e.g. "switch.prod_keys".</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable label shown in the UI.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Longer description / help text.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>What kind of resource this is.</summary>
    public RequirementKind Kind { get; init; } = RequirementKind.FilePath;

    /// <summary>The default / fall-back value (path, URL, …) when the user has not overridden it.</summary>
    public string? DefaultValue { get; init; }

    /// <summary>If <see langword="true"/> the feature degrades gracefully when unmet.</summary>
    public bool IsOptional { get; init; }

    /// <summary>
    /// The currently-resolved value. Set by the UI or auto-resolver at
    /// startup; <see langword="null"/> means "not yet provided".
    /// </summary>
    public string? ResolvedValue { get; set; }

    /// <summary>Quick check – is the requirement currently satisfied?</summary>
    public bool IsMet => Kind switch
    {
        RequirementKind.FilePath or RequirementKind.KeyFile or RequirementKind.DatabaseFile
            => !string.IsNullOrEmpty(EffectiveValue) && File.Exists(EffectiveValue),
        RequirementKind.DirectoryPath
            => !string.IsNullOrEmpty(EffectiveValue) && Directory.Exists(EffectiveValue),
        RequirementKind.Url
            => Uri.TryCreate(EffectiveValue, UriKind.Absolute, out _),
        _ => !string.IsNullOrEmpty(EffectiveValue),
    };

    /// <summary>Returns <see cref="ResolvedValue"/> if set, otherwise <see cref="DefaultValue"/>.</summary>
    public string? EffectiveValue => ResolvedValue ?? DefaultValue;
}

/// <summary>
/// Implemented by ROM parsers, console managers, title-info providers, or
/// plugins that depend on external files / keys / URLs to work correctly.
/// </summary>
public interface IHasRequirements
{
    /// <summary>All requirements this component declares.</summary>
    IReadOnlyList<ConsoleRequirement> GetRequirements();

    /// <summary>Subset of <see cref="GetRequirements"/> whose <see cref="ConsoleRequirement.IsMet"/>
    /// is <see langword="false"/> and <see cref="ConsoleRequirement.IsOptional"/> is
    /// <see langword="false"/>.</summary>
    IReadOnlyList<ConsoleRequirement> GetUnmetRequirements()
        => GetRequirements().Where(r => !r.IsOptional && !r.IsMet).ToList();

    /// <summary>Set the user-provided value for a requirement.</summary>
    void SetRequirementValue(string requirementId, string value)
    {
        var req = GetRequirements().FirstOrDefault(r => r.Id == requirementId);
        if (req is not null) req.ResolvedValue = value;
    }
}
