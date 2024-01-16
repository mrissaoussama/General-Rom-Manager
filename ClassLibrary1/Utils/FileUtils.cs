using RomManagerShared.Switch;
using RomManagerShared.ThreeDS.TitleInfoProviders;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;

namespace RomManagerShared.Utils
{
    public class FileUtils
    {
        public static List<string> ExcludedFolders;
        static readonly string logFilePath = RomManagerConfiguration.GetErrorLogPath();

        static FileUtils()
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
                string fileDirectory = Path.GetDirectoryName(filePath);
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
                Console.WriteLine($"Error moving file: {ex.Message}");
            }
        }

        public static void Log(string logText)
        {
            File.AppendAllText(logFilePath, logText + Environment.NewLine);
        }

    }
}
