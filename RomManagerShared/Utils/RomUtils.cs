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
    public static void CopyNonNullProperties(object source, object destination)
    {
        if (source == null || destination == null)
            throw new ArgumentNullException("Source and destination objects must not be null");

        Type sourceType = source.GetType();
        Type destinationType = destination.GetType();

        PropertyInfo[] sourceProperties = sourceType.GetProperties();
        PropertyInfo[] destinationProperties = destinationType.GetProperties();

        foreach (var sourceProperty in sourceProperties)
        {
            var destinationProperty = destinationProperties.FirstOrDefault(p => p.Name == sourceProperty.Name
                                                                              && p.PropertyType == sourceProperty.PropertyType
                                                                              && p.CanWrite);

            if (destinationProperty != null)
            {
                object value = sourceProperty.GetValue(source);
                if (value != null)
                {
                    destinationProperty.SetValue(destination, value);
                }
            }
        }
    }
    public static void CopyRomProperties(object source, object destination)
    {
        if (source == null || destination == null)
            throw new ArgumentNullException("Source and destination objects must not be null");

        Type sourceType = source.GetType();
        Type destinationType = destination.GetType();

        PropertyInfo[] sourceProperties = sourceType.GetProperties();
        PropertyInfo[] destinationProperties = destinationType.GetProperties();

        foreach (var sourceProperty in sourceProperties)
        {
            var destinationProperty = destinationProperties.FirstOrDefault(p => p.Name == sourceProperty.Name
                                                                              && p.PropertyType == sourceProperty.PropertyType
                                                                              && p.CanWrite);

            if (destinationProperty != null)
            {
                object value = sourceProperty.GetValue(source);
                destinationProperty.SetValue(destination, value);
            }
        }
    }
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
    }    public static string GetRomListSize(List<Rom> romList)
    {
        return romList.Sum(x => x.Size).ToString();
    }

    public static string GetRomBaseFolder(Rom rom)
    {if (rom.Path is null)
            throw new ArgumentException("rom path invalid");
    if (rom.IsFolderFormat is false)
            return rom.Path;
       if(rom is IWiiURom)
        {
            if(rom.Path.ToLower().EndsWith("app.xml"))
            {
                DirectoryInfo directory = Directory.GetParent(Directory.GetParent(rom.Path).FullName);
                return directory.FullName;
            }
            if (rom.Path.ToLower().EndsWith("title.tmd"))
            {
                DirectoryInfo directory = Directory.GetParent(rom.Path);
                return directory.FullName;
            }
        }
       if(rom is IPS3Rom)
        {
            if (rom.Path.ToLower().EndsWith("param.sfo"))
            {
                DirectoryInfo directory = Directory.GetParent(Directory.GetParent(rom.Path).FullName);
                return directory.FullName;
            }
        }
        if (rom is IPSVitaRom)
        {
            if (rom.Path.ToLower().EndsWith("param.sfo"))
            {
                DirectoryInfo directory = Directory.GetParent(Directory.GetParent(rom.Path).FullName);
                return directory.FullName;
            }
        }
        if (rom is IOriginalXboxRom)
        {
            if (rom.Path.ToLower().EndsWith("default.xbe"))
            {
                DirectoryInfo directory = Directory.GetParent(rom.Path);
                return directory.FullName;
            }
        }
        if (rom is IXbox360Rom)
        {
            if (rom.Path.ToLower().EndsWith("default.xex"))
            {
                DirectoryInfo directory = Directory.GetParent(rom.Path);
                return directory.FullName;
            }
        }
        return rom.Path;
    }
}