using RomManagerShared.Base;
namespace RomManagerShared.Utils
{
    public class RomUtils
    {
        static HashSet<string> processedPaths = [];
        public static void CopyRomProperties(Rom source, Rom destination)
        {
            // Set values from source to destination
            destination.TitleID = source.TitleID;
            destination.Version = source.Version;
            destination.Regions = source.Regions;
            destination.Icon = source.Icon;
            destination.Ratings = source.Ratings;
            destination.Publisher = source.Publisher;
            destination.Genres = source.Genres;
            destination.Languages = source.Languages;
            destination.Developer = source.Developer;
            destination.Size = source.Size;
            destination.Descriptions = source.Descriptions;
            destination.MinimumFirmware = source.MinimumFirmware;
            destination.IsDemo = source.IsDemo;
            destination.NumberOfPlayers = source.NumberOfPlayers;
            destination.ReleaseDate = source.ReleaseDate;
            destination.Banner = source.Banner;
            destination.Images = source.Images;
            destination.Path = source.Path;
            destination.Titles = source.Titles;
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
            if (!romList.OfType<Game>().Any())
            {
                folderName = firstRom.TitleID;

            }
            else { folderName = firstRom.Titles?.FirstOrDefault()?.Value ?? firstRom.TitleID; }
            var invalidChars = Path.GetInvalidFileNameChars();
            string cleanedFolderName = new string(folderName
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
        }    }}
