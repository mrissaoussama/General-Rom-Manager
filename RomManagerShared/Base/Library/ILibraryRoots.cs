namespace RomManagerShared.Base.Library;

/// <summary>Where each console's ROMs live. A console can have several folders; the first is its primary one.</summary>
public interface ILibraryRoots
{
    /// <summary>The console's configured folders (existing or not), primary first; empty when none.</summary>
    IReadOnlyList<string> GetRoots(Type console);

    /// <summary>Consoles with at least one configured folder.</summary>
    IReadOnlyList<Type> ConfiguredConsoles();
}
