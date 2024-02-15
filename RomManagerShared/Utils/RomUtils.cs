using RomManagerShared.Base;
using System.Reflection;
namespace RomManagerShared.Utils;

public class RomUtils
{
    static HashSet<string> processedPaths = [];
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
    public static void OrganizeRomsInFolders(HashSet<Rom> romList, HashSet<HashSet<Rom>> groupedRomList, bool organizeGamesOnly = false)
    {
        if (romList == null || romList.Count == 0)
        {
            Console.WriteLine("No ROMs to organize.");
            return;
        }

        Rom? firstRom = romList.OfType<Game>().FirstOrDefault();

        if (firstRom == null)
        {
            if (organizeGamesOnly == true)
            {
                Console.WriteLine("No Game instance found in the list.");
                return;
            }
            else
            {
                firstRom = romList.First();
            }
        }

        string? folderName = string.Empty;
        folderName = !romList.OfType<Game>().Any() ? firstRom.TitleID : firstRom.Titles?.FirstOrDefault()?.Value ?? firstRom.TitleID;
        var invalidChars = Path.GetInvalidFileNameChars();
        string cleanedFolderName = new(folderName
            .Select(c => invalidChars.Contains(c) ? ' ' : c)
            .ToArray());

        string folderPath = Path.GetDirectoryName(firstRom.Path);
        string folderFullPath = Path.Combine(folderPath, cleanedFolderName);

        // Check if the last folder and the one before it have the same name
        string[] folderFullPathnames = folderFullPath.Split(Path.DirectorySeparatorChar);
        if (folderFullPathnames.Length >= 2 && folderFullPathnames[folderFullPathnames.Length - 1] == folderFullPathnames[folderFullPathnames.Length - 2])
        {
            folderFullPath = folderPath;
        }

        foreach (var rom in romList)
        {
            if (rom == null || string.IsNullOrEmpty(rom.Path))
            {
                Console.WriteLine("Invalid ROM found. Skipping.");
                continue;
            }

            string originalFileName = Path.GetFileName(rom.Path);
            string destinationPath = Path.Combine(folderFullPath, originalFileName);

            if (processedPaths.Contains(rom.Path) || destinationPath.Equals(rom.Path, StringComparison.OrdinalIgnoreCase))
            {
                FileUtils.Log("skipped " + rom.Path);
                continue;
            }
            Directory.CreateDirectory(folderFullPath);

            // Check if the destination folder is the same as the source folder
            if (!destinationPath.Equals(rom.Path, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(destinationPath))
                {
                    int count = 1;
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
                    string fileExtension = Path.GetExtension(originalFileName);
                    while (File.Exists(destinationPath))
                    {
                        originalFileName = $"{fileNameWithoutExtension} ({count}){fileExtension}";
                        destinationPath = Path.Combine(folderFullPath, originalFileName);
                        count++;
                    }
                    Console.WriteLine($"File '{originalFileName}' already exists in the folder. Renaming to '{originalFileName}'.");
                }

                try
                {
                    File.Move(rom.Path, destinationPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error moving file '{originalFileName}': {ex.Message}");
                    continue;
                }

                // Mark the path as processed
                processedPaths.Add(rom.Path);

                // Update paths in groupedRomList
                UpdateGroupedRomPaths(groupedRomList, rom.Path, destinationPath);

                Console.WriteLine($"Moved '{originalFileName}' to '{folderName}' folder.");
            }
        }
    }
    private static void UpdateGroupedRomPaths(HashSet<HashSet<Rom>> groupedRomList, string sourcePath, string destinationPath)
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
    }    public static string GetRomListSize(HashSet<Rom> romList)
    {
        return romList.Sum(x => x.Size).ToString();
    }}