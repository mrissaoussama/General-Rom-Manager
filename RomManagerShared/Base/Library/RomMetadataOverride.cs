namespace RomManagerShared.Base.Library;

/// <summary>
/// Metadata set on a rom by a plugin or the user, applied on top of what the parsers found every time the library
/// loads or is scanned. Keyed by console, title ID (or file path for roms without one) and rom type, so moving
/// or rescanning a file keeps it.
/// </summary>
public class RomMetadataOverride
{
    public int Id { get; set; }
    /// <summary>The console type name, as in <see cref="LibraryFile.Console"/>; empty when unknown.</summary>
    public required string Console { get; set; }
    /// <summary>The title ID, or <c>path:&lt;file&gt;</c> for roms without one.</summary>
    public required string RomKey { get; set; }
    /// <summary>Full name of the rom type (game, update and DLC share title IDs on some consoles).</summary>
    public required string RomType { get; set; }

    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Publisher { get; set; }
    public string? Developer { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    /// <summary><see cref="GenreEnum"/> names, comma-separated.</summary>
    public string? Genres { get; set; }
    /// <summary>A cover picture in the app's data folder.</summary>
    public string? CoverPath { get; set; }

    /// <summary>Who set it (a plugin's name).</summary>
    public string? Source { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public RomMetadataOverride Copy() => (RomMetadataOverride)MemberwiseClone();
}
