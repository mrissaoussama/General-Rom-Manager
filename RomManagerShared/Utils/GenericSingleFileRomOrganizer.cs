// File: RomManagerShared/Organizers/GenericSingleFileRomOrganizer.cs
using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
using System.IO;

namespace RomManagerShared.Organizers
{
    public class GenericSingleFileRomOrganizer : IRomOrganizer
    {
        private readonly List<string> _processedPaths = new();

        public string Description { get; set; } = "Used for roms that are packed, not for folders";

        /// <summary>
        /// Organizes ROM files into folders based on their metadata.
        /// </summary>
        /// <param name="romList">The list of ROMs to organize.</param>
        /// <param name="groupedRomList">The grouped list of ROMs for updating paths.</param>
        /// <param name="organizeGamesOnly">If set to true, only Game instances will be organized.</param>
        public void Organize(List<Rom> romList, List<List<Rom>> groupedRomList, bool organizeGamesOnly = false)
        {
            if (romList == null || romList.Count == 0)
            {
                Console.WriteLine("No ROMs to organize.");
                return;
            }

            // Get the first ROM to determine the folder name
            Rom? firstRom = organizeGamesOnly
                ? romList.OfType<Game>().FirstOrDefault()
                : romList.FirstOrDefault();

            if (firstRom == null)
            {
                Console.WriteLine("No suitable ROM found to determine folder name.");
                return;
            }

            // Determine the folder name based on the ROM's TitleID or Titles
            string folderName = romList.OfType<NoIntroGame>().Any()
                ? firstRom.Titles?.FirstOrDefault()?.Value ?? firstRom.TitleID ?? "Unknown"
                : firstRom.TitleID ?? "Unknown";

            // Clean the folder name to remove invalid characters
            string cleanedFolderName = RomUtils.GetCleanName(folderName);

            // Get the base path where the ROMs are located
            string? folderPath = Path.GetDirectoryName(firstRom.Path);
            if (folderPath == null)
            {
                Console.WriteLine("Invalid ROM path.");
                return;
            }

            string folderFullPath = Path.Combine(folderPath, cleanedFolderName);

            // Avoid creating nested folders with the same name
            if (IsNestedFolderSameAsParent(folderFullPath))
            {
                folderFullPath = folderPath;
            }

            // Before creating the directory, check permissions
            if (!HasWritePermission(folderFullPath))
            {
                Console.WriteLine($"Insufficient permissions to write to directory '{folderFullPath}'.");
                return;
            }

            // Check if there's enough free space
            long totalRomSize = romList.Sum(rom => new FileInfo(rom.Path ?? string.Empty).Length);

            if (!HasEnoughDiskSpace(folderFullPath, totalRomSize))
            {
                Console.WriteLine($"Not enough disk space to move ROMs to '{folderFullPath}'.");
                return;
            }

            foreach (var rom in romList)
            {
                if (rom == null)
                {
                    FileUtils.Log("Invalid ROM found. Skipping.");
                    continue;
                }

                if (string.IsNullOrEmpty(rom.Path))
                {
                    FileUtils.Log("Invalid ROM path. Skipping.");
                    continue;
                }

                string originalFileName = Path.GetFileName(rom.Path);
                string destinationPath = Path.Combine(folderFullPath, originalFileName);

                // Skip if the ROM has already been processed or is already in the destination folder
                if (_processedPaths.Contains(rom.Path) || destinationPath.Equals(rom.Path, StringComparison.OrdinalIgnoreCase))
                {
                    FileUtils.Log($"Skipped '{rom.Path}'.");
                    continue;
                }

                try
                {
                    // Create the destination directory if it doesn't exist
                    if (!Directory.Exists(folderFullPath))
                    {
                        Directory.CreateDirectory(folderFullPath);
                    }

                    // Generate a unique destination path if a file with the same name exists
                    string uniqueDestinationPath = GetUniqueDestinationPath(destinationPath);

                    // Move the ROM file to the destination folder
                    File.Move(rom.Path, uniqueDestinationPath);

                    // Mark the path as processed
                    _processedPaths.Add(rom.Path);

                    // Update the ROM's path in groupedRomList
                    UpdateGroupedRomPaths(groupedRomList, rom.Path, uniqueDestinationPath);

                    Console.WriteLine($"Moved '{originalFileName}' to '{cleanedFolderName}' folder.");
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine($"Insufficient permissions to move file '{rom.Path}'.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error moving file '{originalFileName}': {ex.Message}");
                    continue;
                }
            }
        }

        /// <summary>
        /// Checks if a folder path is nested and has the same name as its parent folder.
        /// </summary>
        /// <param name="folderFullPath">The full path of the folder.</param>
        /// <returns>True if the nested folder has the same name as its parent; otherwise, false.</returns>
        private static bool IsNestedFolderSameAsParent(string folderFullPath)
        {
            string[] folderNames = folderFullPath.Split(Path.DirectorySeparatorChar);
            int length = folderNames.Length;
            return length >= 2 &&
                   folderNames[length - 1].Equals(folderNames[length - 2], StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Generates a unique destination path by appending a number if a file with the same name already exists.
        /// </summary>
        /// <param name="destinationPath">The initial destination path.</param>
        /// <returns>A unique destination path.</returns>
        private static string GetUniqueDestinationPath(string destinationPath)
        {
            if (!File.Exists(destinationPath))
            {
                return destinationPath;
            }

            int count = 1;
            string directory = Path.GetDirectoryName(destinationPath) ?? string.Empty;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(destinationPath);
            string fileExtension = Path.GetExtension(destinationPath);

            string newDestinationPath;
            do
            {
                string newFileName = $"{fileNameWithoutExtension} ({count}){fileExtension}";
                newDestinationPath = Path.Combine(directory, newFileName);
                count++;
            } while (File.Exists(newDestinationPath));

            Console.WriteLine($"File '{Path.GetFileName(destinationPath)}' already exists. Renaming to '{Path.GetFileName(newDestinationPath)}'.");

            return newDestinationPath;
        }

        /// <summary>
        /// Updates the file paths in the grouped ROM list after moving a ROM file.
        /// </summary>
        /// <param name="groupedRomList">The list of grouped ROMs.</param>
        /// <param name="sourcePath">The original path of the ROM.</param>
        /// <param name="destinationPath">The new path of the ROM.</param>
        private static void UpdateGroupedRomPaths(List<List<Rom>> groupedRomList, string sourcePath, string destinationPath)
        {
            foreach (var group in groupedRomList)
            {
                foreach (var rom in group)
                {
                    if (rom.Path?.Equals(sourcePath, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        rom.Path = destinationPath;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if there is enough disk space at the destination to move the files.
        /// </summary>
        /// <param name="destinationPath">The destination folder path.</param>
        /// <param name="requiredSpace">The total size of the files to move.</param>
        /// <returns>True if there is enough space; otherwise, false.</returns>
        private static bool HasEnoughDiskSpace(string destinationPath, long requiredSpace)
        {
            try
            {
                string root = Path.GetPathRoot(destinationPath) ?? string.Empty;
                DriveInfo drive = new(root);

                long availableSpace = drive.AvailableFreeSpace;

                return availableSpace > requiredSpace;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking disk space: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if the application has write permission to the specified directory.
        /// </summary>
        /// <param name="directoryPath">The directory to check.</param>
        /// <returns>True if the application has write permissions; otherwise, false.</returns>
        private static bool HasWritePermission(string directoryPath)
        {
            try
            {
                // Attempt to create the directory if it doesn't exist
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Attempt to create a temporary file
                string tempFilePath = Path.Combine(directoryPath, Path.GetRandomFileName());
                using (FileStream fs = File.Create(tempFilePath, 1, FileOptions.DeleteOnClose))
                {
                    // File created successfully, permissions are sufficient
                }

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // Access denied
                return false;
            }
            catch (Exception)
            {
                // Other exceptions can be handled if necessary
                return false;
            }
        }
    }
}