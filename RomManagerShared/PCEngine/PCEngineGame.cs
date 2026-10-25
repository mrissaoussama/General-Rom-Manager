using RomManagerShared.Base;

namespace RomManagerShared.PCEngine;

public class PCEngineGame : Game
{
    [UiProperty(DisplayName = "Header")]
    public string? HeaderFormat { get; set; }

    /// <summary>Copier dumps add a 512-byte header to the 8 KB-aligned ROM.</summary>
    public static bool HasCopierHeader(long length) => length % 8192 == 512;
}
