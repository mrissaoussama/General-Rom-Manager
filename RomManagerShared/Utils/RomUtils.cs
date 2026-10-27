using RomManagerShared.Base;
using RomManagerShared.OriginalXbox;
using RomManagerShared.PS3;
using RomManagerShared.PSVita;
using RomManagerShared.WiiU;
using RomManagerShared.Xbox360;
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

        var file = Path.GetFileName(rom.Path).ToLowerInvariant();
        var parent = Path.GetDirectoryName(rom.Path);
        var grandParent = parent is null ? null : Path.GetDirectoryName(parent);
        var folder = (rom, file) switch
        {
            (IWiiURom, "app.xml") => grandParent,          // <title>/code/app.xml
            (IWiiURom, "title.tmd") => parent,
            (IPS3Rom or IPSVitaRom, "param.sfo") => grandParent, // <title>/sce_sys/param.sfo
            (IOriginalXboxRom, "default.xbe") => parent,
            (IXbox360Rom, "default.xex") => parent,
            _ => null,
        };
        return folder ?? rom.Path;
    }
}
