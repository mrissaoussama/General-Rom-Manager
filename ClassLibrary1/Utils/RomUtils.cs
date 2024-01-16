using RomManagerShared.Base;
using RomManagerShared.ThreeDS.TitleInfoProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.Utils
{
    public class RomUtils
    {
        public static void CopyIRomProperties(Rom source, Rom destination)
        {
            // Set values from source to destination
            destination.TitleID = source.TitleID;
            destination.Version = source.Version;
            destination.Region = source.Region;
            destination.Icon = source.Icon;
            destination.Rating = source.Rating;
            destination.Publisher = source.Publisher;
            destination.RatingContent = source.RatingContent;
            destination.Genres = source.Genres;
            destination.Languages = source.Languages;
            destination.Developer = source.Developer;
            destination.Size = source.Size;
            destination.Description = source.Description;
            destination.MinimumFirmware = source.MinimumFirmware;
            destination.IsDemo = source.IsDemo;
            destination.NumberOfPlayers = source.NumberOfPlayers;
            destination.ReleaseDate = source.ReleaseDate;
            destination.Banner = source.Banner;
            destination.Images = source.Images;
            destination.Path = source.Path;
            destination.TitleName = source.Description;
        }

        public static void OrganizeRomsInFolders(List<Rom> romList)
        {
            if (romList == null || romList.Count == 0)
            {
                Console.WriteLine("No ROMs to organize.");
                return;
            }

            // Find the first IGame instance in the list
            Game firstGame = romList.OfType<Game>().FirstOrDefault();

            if (firstGame == null)
            {
                Console.WriteLine("No IGame instance found in the list.");
                return;
            }

            // Create a folder named after the first IGame's title name
            string folderName = firstGame.TitleName;
            var invalidChars = Path.GetInvalidFileNameChars();
            folderName = new string(folderName
                .Select(c => invalidChars.Contains(c) ? ' ' : c)
                .ToArray());
            string folderPath = Path.GetDirectoryName(firstGame.Path);
            string folderFullPath = Path.Combine(folderPath, folderName);

            // Check if the last folder and the one before it have the same name
            string[] folderFullPathnames = folderFullPath.Split(Path.DirectorySeparatorChar);
            if (folderFullPathnames.Length >= 2 && folderFullPathnames[folderFullPathnames.Length - 1] == folderFullPathnames[folderFullPathnames.Length - 2])
            {
                folderFullPath = folderPath;
            }
            else
            {
                Directory.CreateDirectory(folderFullPath);
            }

            HashSet<string> processedPaths = [];

            foreach (var rom in romList)
            {
                string originalFileName = Path.GetFileName(rom.Path);
                string destinationPath = Path.Combine(folderFullPath, originalFileName);

                if (processedPaths.Contains(destinationPath) || destinationPath == rom.Path)
                {
                    // Skip processing duplicate paths
                    continue;
                }

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

                    File.Move(rom.Path, destinationPath);
                    rom.Path = destinationPath;

                    Console.WriteLine($"Moved '{originalFileName}' to '{folderName}' folder.");
                }

                // Mark the path as processed
                processedPaths.Add(destinationPath);
            }
        }

    }



}
