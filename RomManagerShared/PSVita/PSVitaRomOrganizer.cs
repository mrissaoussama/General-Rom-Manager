using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RomManagerShared.PSVita
{
    public class PSVitaRomOrganizer : IRomOrganizer<PSVitaConsole>
    {
        // Regular expressions for identifying PS Vita content
        private static readonly Regex TitleIdRegex = new Regex(@"(PCS[AEGHIJKLNPSTU][0-9]{5})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex VersionRegex = new Regex(@"\[v?([0-9]+\.[0-9]+)(\.[0-9]+)?\]|\(v?([0-9]+\.[0-9]+)(\.[0-9]+)?\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DlcCountRegex = new Regex(@"\[([0-9]+)\s*DLC\]|\(([0-9]+)\s*DLC\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Standard PS Vita folders
        private static readonly string[] StandardFolders = { "app", "patch", "addcont", "license" };

        // Minimum required free space (100MB)
        private const long MinFreeSpace = 100 * 1024 * 1024;

        private string SourceDirectory { get; set; }
        private string DestinationDirectory { get; set; }
        private bool RemoveEmptyFolders { get; set; }
        private bool DryRun { get; set; }
        public string Description { get; set ; }

        /// <summary>
        /// Organizes PS Vita ROMs into the proper folder structure
        /// </summary>
        public void Organize(string sourceDirectory, string destinationDirectory, bool removeEmptyFolders = true, bool dryRun = false)
        {
            SourceDirectory = sourceDirectory;
            DestinationDirectory = destinationDirectory;
            RemoveEmptyFolders = removeEmptyFolders;
            DryRun = dryRun;
            FileUtils.Log("Starting PS Vita ROM organization");
            FileUtils.Log($"Source: {SourceDirectory}");
            FileUtils.Log($"Destination: {DestinationDirectory}");

            if (DryRun)
            {
                FileUtils.Log("Running in dry-run mode - no files will be modified");
            }

            // Verify directories and permissions
            if (!VerifyDirectories())
                return;

            // Get all directories and files, sorted by depth (deepest first)
            var allPaths = GetAllPaths();

            // Dictionary to track organized game folders by title ID
            var organizedGames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var processedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int gameCount = 0, updateCount = 0, dlcCount = 0, licenseCount = 0, skippedCount = 0;

            // Process all paths from deepest to shallowest
            foreach (var path in allPaths)
            {
                try
                {
                    // Skip if path or parent already processed
                    if (IsAlreadyProcessed(path, processedPaths))
                        continue;

                    // Get the title ID if present
                    string titleId = ExtractTitleId(path);
                    if (string.IsNullOrEmpty(titleId))
                        continue;

                    // Determine content type
                    var contentType = IdentifyContentType(path);
                    if (contentType == ContentType.Unknown)
                        continue;

                    // Check if already organized
                    if (IsProperlyOrganized(path, contentType))
                    {
                        FileUtils.Log($"Skipping already organized content: {path}");

                        // Track organized game location for later reference
                        if (contentType == ContentType.Game)
                        {
                            var gameRootFolder = FindGameRootFolder(path);
                            if (gameRootFolder != null && !organizedGames.ContainsKey(titleId))
                            {
                                organizedGames[titleId] = gameRootFolder;
                            }
                        }

                        // Mark as processed
                        processedPaths.Add(path);
                        skippedCount++;
                        continue;
                    }

                    // Process based on content type
                    switch (contentType)
                    {
                        case ContentType.Game:
                            var gameFolder = OrganizeGame(path, titleId);
                            if (gameFolder != null)
                            {
                                organizedGames[titleId] = gameFolder;
                                gameCount++;
                            }
                            break;

                        case ContentType.Update:
                            if (OrganizeUpdate(path, titleId, organizedGames))
                                updateCount++;
                            break;

                        case ContentType.DLC:
                            if (OrganizeDLC(path, titleId, organizedGames))
                                dlcCount++;
                            break;

                        case ContentType.License:
                            if (OrganizeLicense(path, titleId, organizedGames))
                                licenseCount++;
                            break;
                    }

                    // Mark as processed
                    processedPaths.Add(path);
                }
                catch (Exception ex)
                {
                    FileUtils.Log($"Error processing {path}: {ex.Message}");
                }
            }

            // Remove empty folders if requested
            if (RemoveEmptyFolders && !DryRun)
            {
                RemoveEmptyDirectories(SourceDirectory);
            }

            // Log summary
            FileUtils.Log("Organization complete:");
            FileUtils.Log($"  Games: {gameCount}");
            FileUtils.Log($"  Updates: {updateCount}");
            FileUtils.Log($"  DLCs: {dlcCount}");
            FileUtils.Log($"  Licenses: {licenseCount}");
            FileUtils.Log($"  Skipped (already organized): {skippedCount}");
        }

        /// <summary>
        /// Verifies source and destination directories exist with proper permissions
        /// </summary>
        private bool VerifyDirectories()
        {
            // Check source directory
            if (!Directory.Exists(SourceDirectory))
            {
                FileUtils.Log($"Source directory not found: {SourceDirectory}");
                return false;
            }

            // Create destination if needed
            if (!Directory.Exists(DestinationDirectory) && !DryRun)
            {
                try
                {
                    Directory.CreateDirectory(DestinationDirectory);
                }
                catch (Exception ex)
                {
                    FileUtils.Log($"Failed to create destination directory: {ex.Message}");
                    return false;
                }
            }

            // Check write permissions
            if (!DryRun)
            {
                try
                {
                    string testFile = Path.Combine(DestinationDirectory, "writetest.tmp");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                }
                catch (Exception ex)
                {
                    FileUtils.Log($"No write permission to destination: {ex.Message}");
                    return false;
                }
            }

            // Check free space
            var drive = new DriveInfo(Path.GetPathRoot(DestinationDirectory));
            if (drive.AvailableFreeSpace < MinFreeSpace)
            {
                FileUtils.Log($"Insufficient disk space: {FileUtils.FormatFileSize(drive.AvailableFreeSpace)} available");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets all paths sorted by depth (deepest first) to handle nested content
        /// </summary>
        private List<string> GetAllPaths()
        {
            var result = new List<string>();

            // Add directories
            result.AddRange(Directory.GetDirectories(SourceDirectory, "*", SearchOption.AllDirectories)
                .OrderByDescending(p => p.Count(c => c == Path.DirectorySeparatorChar)));

            // Add individual license files
            result.AddRange(Directory.GetFiles(SourceDirectory, "*.rif", SearchOption.AllDirectories));

            return result;
        }

        /// <summary>
        /// Checks if a path or its parent has already been processed
        /// </summary>
        private bool IsAlreadyProcessed(string path, HashSet<string> processedPaths)
        {
            if (processedPaths.Contains(path))
                return true;

            // Check if any parent directory has been processed
            string currentPath = path;
            while (!string.IsNullOrEmpty(currentPath))
            {
                currentPath = Path.GetDirectoryName(currentPath);
                if (string.IsNullOrEmpty(currentPath))
                    break;

                if (processedPaths.Contains(currentPath))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Extracts PS Vita title ID from path
        /// </summary>
        private string ExtractTitleId(string path)
        {
            var match = TitleIdRegex.Match(path);
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// Identifies the type of content (Game, Update, DLC, License)
        /// </summary>
        private ContentType IdentifyContentType(string path)
        {
            // Special case for license files
            if (File.Exists(path) && Path.GetExtension(path).Equals(".rif", StringComparison.OrdinalIgnoreCase))
                return ContentType.License;

            // Check path string for indicators
            string pathLower = path.ToLowerInvariant();

            // Check for existing organized structure
            if (pathLower.EndsWith("\\app") || pathLower.Contains("\\app\\"))
                return ContentType.Game;

            if (pathLower.EndsWith("\\patch") || pathLower.Contains("\\patch\\"))
                return ContentType.Update;

            if (pathLower.Contains("\\addcont\\"))
                return ContentType.DLC;

            if (pathLower.EndsWith("\\license") || pathLower.Contains("\\license\\"))
                return ContentType.License;

            // Check for game indicators
            if (Directory.Exists(path))
            {
                // Check for eboot.bin (main game executable)
                if (File.Exists(Path.Combine(path, "eboot.bin")))
                    return ContentType.Game;

                // Check for sce_sys directory (common in all Vita content)
                if (Directory.Exists(Path.Combine(path, "sce_sys")))
                {
                    // Look for indicators in the path
                    if (pathLower.Contains("patch") || pathLower.Contains("update"))
                        return ContentType.Update;

                    if (pathLower.Contains("dlc") || pathLower.Contains("addcont"))
                        return ContentType.DLC;

                    // Default to game if sce_sys exists
                    return ContentType.Game;
                }
            }

            // Check path name for indicators
            if (pathLower.Contains("patch") || pathLower.Contains("update"))
                return ContentType.Update;

            if (pathLower.Contains("dlc") || pathLower.Contains("addcont"))
                return ContentType.DLC;

            if (pathLower.Contains("license") || pathLower.Contains("rif"))
                return ContentType.License;

            // If title ID exists but type is unclear, default to game
            return ContentType.Game;
        }

        /// <summary>
        /// Checks if content is already properly organized
        /// </summary>
        private bool IsProperlyOrganized(string path, ContentType contentType)
        {
            // Check if this is a single file
            if (File.Exists(path))
                return false;

            // For each content type, check if it's in the correct structure
            switch (contentType)
            {
                case ContentType.Game:
                    return path.EndsWith("\\app") && File.Exists(Path.Combine(path, "eboot.bin"));

                case ContentType.Update:
                    return path.EndsWith("\\patch");

                case ContentType.DLC:
                    return path.Contains("\\addcont\\");

                case ContentType.License:
                    return path.EndsWith("\\license");
            }

            return false;
        }

        /// <summary>
        /// Finds the root game folder for an organized structure
        /// </summary>
        private string FindGameRootFolder(string path)
        {
            // If it's the app folder, return parent
            if (path.EndsWith("\\app"))
                return Directory.GetParent(path)?.FullName;

            // If inside app folder, navigate up
            var pathParts = path.Split(Path.DirectorySeparatorChar);
            for (int i = pathParts.Length - 1; i >= 0; i--)
            {
                if (pathParts[i].Equals("app", StringComparison.OrdinalIgnoreCase))
                {
                    // Return parent of app folder
                    return string.Join(Path.DirectorySeparatorChar.ToString(), pathParts.Take(i));
                }
            }

            return null;
        }

        /// <summary>
        /// Organizes a PS Vita game
        /// </summary>
        private string OrganizeGame(string sourcePath, string titleId)
        {
            try
            {
                // Extract name from path
                string gameName = ExtractNameFromPath(sourcePath);

                // Create destination folder
                string gameFolder = Path.Combine(DestinationDirectory, $"{gameName} [{titleId}]");
                gameFolder = EnsureUniqueDirectory(gameFolder);

                string appFolder = Path.Combine(gameFolder, "app");

                if (DryRun)
                {
                    FileUtils.Log($"[DRY RUN] Would organize game {titleId} to {appFolder}");
                    return gameFolder;
                }

                // Create app folder
                Directory.CreateDirectory(appFolder);

                // Move content
                CopyContent(sourcePath, appFolder);

                FileUtils.Log($"Organized game {titleId} to {appFolder}");
                return gameFolder;
            }
            catch (Exception ex)
            {
                FileUtils.Log($"Failed to organize game {titleId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Organizes a PS Vita update
        /// </summary>
        private bool OrganizeUpdate(string sourcePath, string titleId, Dictionary<string, string> organizedGames)
        {
            try
            {
                string patchFolder;

                if (organizedGames.TryGetValue(titleId, out string gameFolder))
                {
                    // Update belongs to known game
                    patchFolder = Path.Combine(gameFolder, "patch");
                }
                else
                {
                    // Standalone update
                    string updateName = ExtractNameFromPath(sourcePath);
                    string updateFolder = Path.Combine(DestinationDirectory, $"{updateName} [{titleId}] [Update]");
                    updateFolder = EnsureUniqueDirectory(updateFolder);
                    patchFolder = Path.Combine(updateFolder, "patch");
                }

                if (DryRun)
                {
                    FileUtils.Log($"[DRY RUN] Would organize update {titleId} to {patchFolder}");
                    return true;
                }

                // Create folder and copy content
                Directory.CreateDirectory(patchFolder);
                CopyContent(sourcePath, patchFolder);

                FileUtils.Log($"Organized update {titleId} to {patchFolder}");
                return true;
            }
            catch (Exception ex)
            {
                FileUtils.Log($"Failed to organize update {titleId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Organizes a PS Vita DLC
        /// </summary>
        private bool OrganizeDLC(string sourcePath, string titleId, Dictionary<string, string> organizedGames)
        {
            try
            {
                // For DLCs, try to determine the game ID from the DLC ID
                string gameId = titleId.Substring(0, 9); // First 9 chars (PCSE00123)
                string dlcFolder;

                if (organizedGames.TryGetValue(gameId, out string gameFolder))
                {
                    // DLC belongs to known game
                    string addcontFolder = Path.Combine(gameFolder, "addcont");
                    dlcFolder = Path.Combine(addcontFolder, titleId);
                }
                else
                {
                    // Standalone DLC
                    string dlcName = ExtractNameFromPath(sourcePath);
                    string dlcRootFolder = Path.Combine(DestinationDirectory, $"{dlcName} [{titleId}] [DLC]");
                    dlcRootFolder = EnsureUniqueDirectory(dlcRootFolder);
                    string addcontFolder = Path.Combine(dlcRootFolder, "addcont");
                    dlcFolder = Path.Combine(addcontFolder, titleId);
                }

                if (DryRun)
                {
                    FileUtils.Log($"[DRY RUN] Would organize DLC {titleId} to {dlcFolder}");
                    return true;
                }

                // Create folders and copy content
                Directory.CreateDirectory(dlcFolder);
                CopyContent(sourcePath, dlcFolder);

                FileUtils.Log($"Organized DLC {titleId} to {dlcFolder}");
                return true;
            }
            catch (Exception ex)
            {
                FileUtils.Log($"Failed to organize DLC {titleId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Organizes a PS Vita license
        /// </summary>
        private bool OrganizeLicense(string sourcePath, string titleId, Dictionary<string, string> organizedGames)
        {
            try
            {
                string licenseFolder;

                if (organizedGames.TryGetValue(titleId, out string gameFolder))
                {
                    // License belongs to known game
                    licenseFolder = Path.Combine(gameFolder, "license");
                }
                else
                {
                    // Standalone license
                    string licenseName = ExtractNameFromPath(sourcePath);
                    string licenseRootFolder = Path.Combine(DestinationDirectory, $"{licenseName} [{titleId}] [License]");
                    licenseRootFolder = EnsureUniqueDirectory(licenseRootFolder);
                    licenseFolder = Path.Combine(licenseRootFolder, "license");
                }

                if (DryRun)
                {
                    FileUtils.Log($"[DRY RUN] Would organize license {titleId} to {licenseFolder}");
                    return true;
                }

                // Create license folder
                Directory.CreateDirectory(licenseFolder);

                // Handle single .rif file
                if (File.Exists(sourcePath) && Path.GetExtension(sourcePath).Equals(".rif", StringComparison.OrdinalIgnoreCase))
                {
                    string destFile = Path.Combine(licenseFolder, Path.GetFileName(sourcePath));
                    if (File.Exists(destFile))
                    {
                        destFile = GetUniqueFilePath(destFile);
                    }
                    File.Copy(sourcePath, destFile, false);
                }
                else
                {
                    // Copy directory contents
                    CopyContent(sourcePath, licenseFolder);
                }

                FileUtils.Log($"Organized license {titleId} to {licenseFolder}");
                return true;
            }
            catch (Exception ex)
            {
                FileUtils.Log($"Failed to organize license {titleId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Extracts a clean name from path for folder naming
        /// </summary>
        private string ExtractNameFromPath(string path)
        {
            string name;

            if (Directory.Exists(path))
            {
                name = new DirectoryInfo(path).Name;
            }
            else if (File.Exists(path))
            {
                name = Path.GetFileNameWithoutExtension(path);
            }
            else
            {
                name = Path.GetFileName(path);
            }

            // Clean up name by removing patterns
            name = TitleIdRegex.Replace(name, "");
            name = VersionRegex.Replace(name, "");
            name = DlcCountRegex.Replace(name, "");

            // Remove common labels
            name = name.Replace("[Update]", "").Replace("[DLC]", "").Replace("[License]", "");
            name = name.Replace("(Update)", "").Replace("(DLC)", "").Replace("(License)", "");

            // Clean up whitespace
            name = name.Trim();
            while (name.Contains("  "))
                name = name.Replace("  ", " ");

            // Use Unknown if empty
            if (string.IsNullOrWhiteSpace(name))
                name = "Unknown";

            return name;
        }

        /// <summary>
        /// Ensures directory path is unique by adding number suffix if needed
        /// </summary>
        private string EnsureUniqueDirectory(string path)
        {
            if (!Directory.Exists(path) || DryRun)
                return path;

            int counter = 1;
            string newPath;
            do
            {
                newPath = $"{path} ({counter})";
                counter++;
            } while (Directory.Exists(newPath));

            return newPath;
        }

        /// <summary>
        /// Gets unique path for file by adding number suffix if needed
        /// </summary>
        private string GetUniqueFilePath(string filePath)
        {
            if (!File.Exists(filePath) || DryRun)
                return filePath;

            string dir = Path.GetDirectoryName(filePath);
            string name = Path.GetFileNameWithoutExtension(filePath);
            string ext = Path.GetExtension(filePath);

            int counter = 1;
            string newPath;
            do
            {
                newPath = Path.Combine(dir, $"{name} ({counter}){ext}");
                counter++;
            } while (File.Exists(newPath));

            return newPath;
        }

        /// <summary>
        /// Copies content from source to destination
        /// </summary>
        private void CopyContent(string source, string destination)
        {
            // Handle single file
            if (File.Exists(source))
            {
                string destFile = Path.Combine(destination, Path.GetFileName(source));
                if (File.Exists(destFile))
                {
                    if (FileUtils.AreFilesEqual(source, destFile))
                    {
                        FileUtils.Log($"Skipping identical file: {Path.GetFileName(source)}");
                        return;
                    }
                    destFile = GetUniqueFilePath(destFile);
                }
                File.Copy(source, destFile, false);
                return;
            }

            // Create destination directory
            Directory.CreateDirectory(destination);

            // Copy all files
            foreach (string file in Directory.GetFiles(source))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destination, fileName);

                if (File.Exists(destFile))
                {
                    if (FileUtils.AreFilesEqual(file, destFile))
                    {
                        FileUtils.Log($"Skipping identical file: {fileName}");
                        continue;
                    }
                    destFile = GetUniqueFilePath(destFile);
                }

                File.Copy(file, destFile, false);
            }

            // Process subdirectories
            foreach (string dir in Directory.GetDirectories(source))
            {
                string dirName = Path.GetFileName(dir);
                string destDir = Path.Combine(destination, dirName);
                CopyContent(dir, destDir);
            }
        }

        /// <summary>
        /// Recursively removes empty directories
        /// </summary>
        private void RemoveEmptyDirectories(string directory)
        {
            // Process all subdirectories first
            foreach (string dir in Directory.GetDirectories(directory))
            {
                RemoveEmptyDirectories(dir);
            }

            // Check if this directory is now empty
            if (Directory.GetFiles(directory).Length == 0 &&
                Directory.GetDirectories(directory).Length == 0)
            {
                try
                {
                    Directory.Delete(directory);
                    FileUtils.Log($"Removed empty directory: {directory}");
                }
                catch (Exception ex)
                {
                    FileUtils.Log($"Could not remove directory: {ex.Message}");
                }
            }
        }

        public void Organize(List<Rom> romList, List<List<Rom>> groupedRomList, bool organizeGamesOnly = false)
        {
            throw new NotImplementedException();
        }

        private enum ContentType
        {
            Unknown,
            Game,
            Update,
            DLC,
            License
        }
    }
}