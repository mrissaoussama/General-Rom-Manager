using RomManagerShared.Base;
using System.Reflection;
namespace RomManagerShared.Utils;

public class RomUtils
{
    static List<string> processedPaths = [];
    /// <summary>
    /// Cleans a folder or file name by removing invalid characters.
    /// </summary>
    /// <param name="name">The name to clean.</param>
    /// <returns>A clean name without invalid characters.</returns>
    public static string GetCleanName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        char[] invalidChars = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalidChars.Contains(c) ? ' ' : c).ToArray());
    }

    public static void UpdateGroupedRomPaths(List<List<Rom>> groupedRomList, string sourcePath, string destinationPath)
    {
        var list = groupedRomList.ToList();

        for (int i = 0; i < list.Count; i++)
        {
            for (int j = 0; j < list[i].Count; j++)
            {
                var group = list[i].ToList();
                if (group[j].Path == sourcePath)
                {
                    group[j].Path = destinationPath;
                }
            }
        }
    }
    public static string GetRomListSize(List<Rom> romList)
    {
        return (romList.Sum(x => x.Size) ?? 0).ToString();
    }

    /// <summary>The folder that holds a folder-format rom, found from the marker file the parser opened.</summary>
    public static string GetRomBaseFolder(Rom rom)
    {
        if (rom.Path is null)
            throw new ArgumentException("rom path invalid");
        if (!rom.IsFolderFormat)
            return rom.Path;

        var levels = rom is IFolderLayout layout ? layout.FolderLevelsAboveMarker(Path.GetFileName(rom.Path).ToLowerInvariant()) : 0;
        if (levels <= 0) return rom.Path;
        string? folder = rom.Path;
        for (int i = 0; i < levels && folder is not null; i++) folder = Path.GetDirectoryName(folder);
        return folder ?? rom.Path;
    }
}
