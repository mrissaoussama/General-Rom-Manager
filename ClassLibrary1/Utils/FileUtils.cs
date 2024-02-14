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
    {
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

}
