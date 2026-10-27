using Microsoft.Extensions.Logging;
using RomManagerShared.Configuration;
using System.Diagnostics;

namespace RomManagerShared.Utils;

public class FileUtils
{
    /// <summary>
    /// Optional structured logger. When set (wired via DI in App.axaml.cs),
    /// log messages are forwarded to the ILogger sink in addition to the flat file.
    /// </summary>
    public static ILogger? Logger { get; set; }

    public static List<string> ExcludedFolders;

    private static string GetLogFilePath()
    {
        try { return RomManagerConfiguration.GetErrorLogPath(); }
        catch
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GeneralRomManager", "Logs", "errors.log");
        }
    }

    static FileUtils()
    {
        ExcludedFolders = ["ErrorFiles"];
    }
    /// <summary>Read-only, shared open. Use for all ROM access.</summary>
    public static FileStream OpenRead(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read);

    /// <summary>
    /// Read-only view of a disc image; transparently decompresses .cso, .zso and .chd.
    /// CHD CD images keep their 2448-byte frames, so read them with <see cref="RawCdReader"/>.
    /// </summary>
    public static Stream OpenDiscImage(string path)
    {
        if (path.EndsWith(".chd", StringComparison.OrdinalIgnoreCase))
        {
            var error = CHDSharp.ChdFile.OpenAsStream(path, out var chd, CancellationToken.None);
            return chd ?? throw new InvalidDataException($"CHD open failed: {error}");
        }
        var file = OpenRead(path);
        if (!path.EndsWith(".cso", StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".zso", StringComparison.OrdinalIgnoreCase))
            return file;
        return CsoStream.TryOpen(file) ?? throw new InvalidDataException("Unsupported CSO/ZSO variant");
    }

    /// <summary>Scratch folder under the cache (with trailing separator). Never the ROM folder.</summary>
    public static string GetTempDirectory()
    {
        var cache = RomManagerConfiguration.GetCachePath();
        var dir = Path.Combine(string.IsNullOrEmpty(cache) ? Path.GetTempPath() : cache, "Temp");
        Directory.CreateDirectory(dir);
        return dir + Path.DirectorySeparatorChar;
    }

    public static long GetFileSize(string filePath)
    {
        if (File.Exists(filePath))
        {
            FileInfo fileInfo = new(filePath);
            return fileInfo.Length; // Size in bytes
        }
        return -1; // Return -1 if the file doesn't exist
    }
    public static List<string> GetFilesInDirectoryWithExtensions(string directory, IEnumerable<string> extensions)
    {
        var results = Directory
            .EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
            .Where(s =>
            {
                var fileExtension = Path.GetExtension(s).TrimStart('.').ToLowerInvariant();
                var folderName = Path.GetDirectoryName(s);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                return extensions.Contains(fileExtension) && !ExcludedFolders.Any(folderName.Contains);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            })
            .ToList();
        return results;
    }
    public static void MoveFileToErrorFiles(string filePath)
    {
        try
        {
            string fileName = Path.GetFileName(filePath);
            string fileDirectory = Path.GetDirectoryName(filePath) ?? "";
            string errorFilesDirectory = Path.Combine(fileDirectory, "ErrorFiles");
            Directory.CreateDirectory(errorFilesDirectory);
            string errorFilePath = Path.Combine(errorFilesDirectory, fileName);
            // Check if the file already exists in the ErrorFiles directory
            int fileCounter = 1;
            while (File.Exists(errorFilePath))
            {
                string newFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{fileCounter}{Path.GetExtension(fileName)}";
                errorFilePath = Path.Combine(errorFilesDirectory, newFileName);
                fileCounter++;
            }
            File.Move(filePath, errorFilePath);
        }
        catch (Exception ex)
        {
            FileUtils.Log($"Error moving file: {ex.Message}");
        }
    }
    /// <summary>Fired on the calling thread whenever a new log entry is added.</summary>
    public static event Action<string>? LogAdded;

    public static void Log(string logText)
    {
        // Keep an in-memory ring-buffer of the last 500 messages for the LogWindow.
        var entry = $"[{DateTime.Now:HH:mm:ss}] {logText}";
        ParseDiagnostics.Add(logText);
        lock (_recentLogs)
        {
            _recentLogs.Add(entry);
            if (_recentLogs.Count > 500)
                _recentLogs.RemoveAt(0);
        }

        // Notify subscribers (UI can marshal to dispatcher)
        LogAdded?.Invoke(entry);

        // Write directly to the flat log file.
        // Do NOT forward to Logger here — FileUtilsLoggerProvider already bridges
        // Microsoft.Extensions.Logging → FileUtils.Log(), so calling Logger here
        // would create an infinite feedback loop.
        try
        {
            var logFilePath = GetLogFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
            File.AppendAllText(logFilePath, entry + Environment.NewLine);
        }
        catch { /* swallow — don't let logging failures crash the app */ }
    }

    /// <summary>
    /// Thread-safe snapshot of recent log lines (last 500). Used by the Log window.
    /// </summary>
    public static IReadOnlyList<string> GetRecentLogs()
    {
        lock (_recentLogs) return _recentLogs.ToList();
    }

    /// <summary>Path to the active log file.</summary>
    public static string LogFilePath => GetLogFilePath();

    private static readonly List<string> _recentLogs = [];
    /// <summary>
    /// Format bytes to human-readable size
    /// </summary>
    /// <param name="bytes">The size in bytes</param>
    /// <returns>Human-readable size string</returns>
    public static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;

        // Divide by 1024 until the number is smaller than 1024 or we run out of suffixes
        while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
        {
            number /= 1024;
            counter++;
        }

        // Format the number to 2 decimal places and append the appropriate suffix
        return $"{number:n2} {suffixes[counter]}";
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
        string parentFolderName = Path.GetFileName(Path.GetDirectoryName(nestedFolderPath)) ?? "";
        if (string.IsNullOrEmpty(parentFolderName))
        {
            return false;
        }
        string nestedFolderName = Path.GetFileName(nestedFolderPath);
        return string.Equals(parentFolderName, nestedFolderName, StringComparison.OrdinalIgnoreCase);
    }


    public static bool DriveHasEnoughSpace(long fileSize, string destinationFolder)
    {
        // Check if there is enough space in the destination folder
        DriveInfo drive = new DriveInfo(Path.GetPathRoot(destinationFolder) ?? destinationFolder);
        return drive.AvailableFreeSpace > fileSize;
    }

    /// <summary>Byte-for-byte comparison (lengths first).</summary>
    public static bool AreFilesEqual(string file1, string file2)
    {
        if (!File.Exists(file1) || !File.Exists(file2)) return false;
        if (new FileInfo(file1).Length != new FileInfo(file2).Length) return false;
        using var fs1 = OpenRead(file1);
        using var fs2 = OpenRead(file2);
        var b1 = new byte[81920];
        var b2 = new byte[81920];
        int read;
        while ((read = fs1.Read(b1, 0, b1.Length)) > 0)
        {
            fs2.ReadExactly(b2, 0, read);
            if (!b1.AsSpan(0, read).SequenceEqual(b2.AsSpan(0, read))) return false;
        }
        return true;
    }
}
