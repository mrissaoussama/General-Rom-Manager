namespace RomManagerShared.Base.Library;

/// <summary>One scanned file and the roms parsed from it.</summary>
public class LibraryFile
{
    public int Id { get; set; }
    public required string Console { get; set; }
    public required string Path { get; set; }
    public long Size { get; set; }
    public DateTime LastWriteUtc { get; set; }
    public int ScanVersion { get; set; }
    public DateTime ScannedUtc { get; set; }
    /// <summary>Polymorphic JSON from <see cref="RomSnapshot"/>.</summary>
    public string RomsJson { get; set; } = "[]";
}
