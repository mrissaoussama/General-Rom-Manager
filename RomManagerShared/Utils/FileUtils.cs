using RomManagerShared.Configuration;
using System.Diagnostics;

namespace RomManagerShared.Utils;

public class FileUtils
{
    public static List<string> ExcludedFolders;
    static readonly string logFilePath = RomManagerConfiguration.GetErrorLogPath();    static FileUtils()
    {
        ExcludedFolders =
        [
            "ErrorFiles"
        ];
    }
    public static long GetFileSize(string filePath)
    {
        if (File.Exists(filePath))
        {
            FileInfo fileInfo = new(filePath);
            return fileInfo.Length; // Size in bytes
        }        return -1; // Return -1 if the file doesn't exist
    }
    public static List<string> GetFilesInDirectoryWithExtensions(string directory, IEnumerable<string> extensions)
    {
        var results = Directory
            .EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
            .Where(s =>
            {
                var fileExtension = Path.GetExtension(s).TrimStart('.').ToLowerInvariant();
                var folderName = Path.GetDirectoryName(s);#pragma warning disable CS8602 // Dereference of a possibly null reference.
                return extensions.Contains(fileExtension) && !ExcludedFolders.Any(folderName.Contains);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            })
            .ToList();        return results;
    }    public static void MoveFileToErrorFiles(string filePath)
    {
        try
        {
            string fileName = Path.GetFileName(filePath);
            string fileDirectory = Path.GetDirectoryName(filePath);
            string errorFilesDirectory = Path.Combine(fileDirectory, "ErrorFiles");            Directory.CreateDirectory(errorFilesDirectory);            string errorFilePath = Path.Combine(errorFilesDirectory, fileName);            // Check if the file already exists in the ErrorFiles directory
            int fileCounter = 1;
            while (File.Exists(errorFilePath))
            {
                string newFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{fileCounter}{Path.GetExtension(fileName)}";
                errorFilePath = Path.Combine(errorFilesDirectory, newFileName);
                fileCounter++;
            }            File.Move(filePath, errorFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error moving file: {ex.Message}");
        }
    }    public static void Log(string logText)
    {// create dir and file if they don't exist
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
        File.AppendAllText(logFilePath, logText + Environment.NewLine);
    }
    public static string CheckForPython()
    {
        ProcessStartInfo pycheck = new()
        {
            FileName = @"python.exe",
            Arguments = "--version",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using Process process = Process.Start(pycheck);
        using StreamReader reader = process.StandardOutput;
        string result = reader.ReadToEnd();
        result = result.Replace("\r\n", "");
        result = result.Replace("Python ", "");
        return result;
    }
    public static bool IsPythonVersionValid(string version, string targetVersion,out string invalidReason)
    {
        invalidReason = string.Empty;
        string[] currentParts = version.Split('.');
        string[] targetParts = targetVersion.Split('.');

        if (currentParts.Length >= 2 && targetParts.Length >= 2)
        {
            int currentMajor = int.Parse(currentParts[0]);
            int currentMinor = int.Parse(currentParts[1]);

            int targetMajor = int.Parse(targetParts[0]);
            int targetMinor = int.Parse(targetParts[1]);

            if (currentMajor > targetMajor || (currentMajor == targetMajor && currentMinor >= targetMinor))
            {
                return true;
            }
        }
        return false;
    }

  
    /// <summary>
    /// Checks if the application has write permission to the specified path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if write permission is granted; otherwise, false.</returns>
    public static bool HasWritePermission(string path)
    {
        try
        {
            // Try to create and delete a temporary file in the directory
            string testFilePath = Path.Combine(path, Path.GetRandomFileName());
            using (FileStream fs = File.Create(testFilePath, 1, FileOptions.DeleteOnClose))
            {
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the nested folder has the same name as its parent folder.
    /// </summary>
    /// <param name="nestedFolderPath">The nested folder path.</param>
    /// <returns>True if the nested folder name is the same as the parent; otherwise, false.</returns>

    public static bool IsNestedFolder(string nestedFolderPath)
    {
        string parentFolderName = Path.GetFileName(Path.GetDirectoryName(nestedFolderPath));
        if (string.IsNullOrEmpty(parentFolderName))
        {
            return false;
        }
        string nestedFolderName = Path.GetFileName(nestedFolderPath);
        return string.Equals(parentFolderName, nestedFolderName, StringComparison.OrdinalIgnoreCase);
    }

    public static void InstallPythonPackages(string requirementsFilePath)
    {
        try
        {
            ProcessStartInfo pipInstall = new()
            {
                FileName = "pip", // Assuming pip is in the system PATH
                Arguments = $"install -r \"{requirementsFilePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using Process process = Process.Start(pipInstall);
            process.WaitForExit(); // Wait for the process to finish

            // Output the result
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            Console.WriteLine("Output:");
            Console.WriteLine(output);
            Console.WriteLine("Error:");
            Console.WriteLine(error);

            if (process.ExitCode == 0)
            {
                Console.WriteLine("Packages installed successfully.");
            }
            else
            {
                Console.WriteLine("Failed to install packages.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    public static bool DriveHasEnoughSpace(long fileSize, string destinationFolder)
    {
        // Check if there is enough space in the destination folder
        DriveInfo drive = new DriveInfo(Path.GetPathRoot(destinationFolder));
        return drive.AvailableFreeSpace > fileSize;
    }

    public static void MoveDirectoryContents(string sourceDir, string destDir)
    {
        // Create all subdirectories first with conflict resolution
        foreach (string dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            string targetDir = dir.Replace(sourceDir, destDir);
            Directory.CreateDirectory(targetDir);
        }

        // Move all files with conflict resolution
        foreach (string file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            string destFile = file.Replace(sourceDir, destDir);

            // Handle file conflicts
            if (File.Exists(destFile))
            {
                string fileName = Path.GetFileNameWithoutExtension(destFile);
                string extension = Path.GetExtension(destFile);
                int counter = 1;

                do
                {
                    string newFileName = $"{fileName} ({counter++}){extension}";
                    destFile = Path.Combine(Path.GetDirectoryName(destFile), newFileName);
                } while (File.Exists(destFile));
            }

            File.Move(file, destFile);
        }

        // Delete source directory after verification
        if (Directory.GetFiles(sourceDir).Length == 0 &&
            Directory.GetDirectories(sourceDir).Length == 0)
        {
            Directory.Delete(sourceDir, true);
        }
        else
        {
            
            Log($"directory {sourceDir} not empty");
        }
    }

    public static void MoveDirectoryCrossPlatform(string sourceDir, string destDir)
    {
        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

        // Check if source and destination directories are equal
        if (AreDirectoriesEqual(sourceDir, destDir))
        {
            Console.WriteLine("Source and destination directories are the same. Skipping move operation.");
            return;
        }

        // If the destination directory exists, rename it
        if (Directory.Exists(destDir))
        {
            destDir = GetUniqueDirectoryName(destDir);
        }

        // Check if we're moving across drives
        var sourceDrive = Path.GetPathRoot(Path.GetFullPath(sourceDir));
        var destDrive = Path.GetPathRoot(Path.GetFullPath(destDir));

        if (string.Equals(sourceDrive, destDrive, StringComparison.OrdinalIgnoreCase))
        {
            // Same drive - use native move
            Directory.Move(sourceDir, destDir);
        }
        else
        {
            // Cross-drive - copy and delete
            CopyDirectory(sourceDir, destDir);
            Directory.Delete(sourceDir, true);
        }
    }

    /// <summary>
    /// Checks if two directories are equal by comparing their full paths.
    /// </summary>
    private static bool AreDirectoriesEqual(string dir1, string dir2)
    {
        return string.Equals(
            Path.GetFullPath(dir1).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(dir2).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase
        );
    }

    /// <summary>
    /// Generates a unique directory name by appending (2), (3), etc., until a unique name is found.
    /// </summary>
    private static string GetUniqueDirectoryName(string destDir)
    {
        string newDestDir = destDir;
        int counter = 2;

        while (Directory.Exists(newDestDir))
        {
            newDestDir = $"{destDir} ({counter})";
            counter++;
        }

        return newDestDir;
    }

    /// <summary>
    /// Recursively copies a directory from source to destination.
    /// </summary>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile, true); // Overwrite if file already exists
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(dir);
            string destSubDir = Path.Combine(destDir, dirName);
            CopyDirectory(dir, destSubDir);
        }
    }}
