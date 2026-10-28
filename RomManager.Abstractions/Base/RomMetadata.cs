namespace RomManagerShared.Base;

/// <summary>
/// Metadata a plugin (or the user) sets on a rom through <see cref="IPluginContext.UpdateMetadataAsync"/>. Null
/// fields are left as they are. The values are kept separately from scan results, so they survive rescans and
/// moved files, and the change can be undone.
/// </summary>
public sealed record RomMetadata
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Publisher { get; init; }
    public string? Developer { get; init; }
    public DateOnly? ReleaseDate { get; init; }

    /// <summary>Genre names ("Action", "Role-playing (RPG)", "Platformer"…); names the app does not know are ignored.</summary>
    public IReadOnlyList<string>? Genres { get; init; }

    /// <summary>A cover picture (PNG, JPEG, WebP or BMP bytes); stored in the app's data folder.</summary>
    public byte[]? CoverImage { get; init; }

    public bool IsEmpty => Title is null && Description is null && Publisher is null && Developer is null
        && ReleaseDate is null && Genres is null && CoverImage is null;
}
