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

namespace RomManagerShared
{
    public class FileUtils
    {
        public static List<string> ExcludedFolders;
       static string logFilePath =RomManagerConfiguration.GetErrorLogPath() ;

         static FileUtils()
        {
            ExcludedFolders = new()
            {
                "ErrorFiles"
            };
        }

        public static List<string> GetFilesInDirectoryWithExtensions(string directory, List<string> extensions)
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
        public static void SaveThumbnail(SwitchGameMetaData nspMetaData, string filePath)
        {
            using (Image thumbnailImage = Base64StringToImage(nspMetaData.Thumbnail))
            {
                var thumbnailPath = filePath + ".jpg";
                Console.WriteLine($"Saved thumbnail to: {thumbnailPath}!");
#pragma warning disable CA1416 // Validate platform compatibility
                thumbnailImage.Save(thumbnailPath);
#pragma warning restore CA1416 // Validate platform compatibility
            }
        }

        public static Image Base64StringToImage(string input)
        {
            var bytes = Convert.FromBase64String(input);
            var stream = new MemoryStream(bytes);
#pragma warning disable CA1416 // Validate platform compatibility
            return Image.FromStream(stream);
#pragma warning restore CA1416 // Validate platform compatibility
        }
        public static string SHA256Bytes(byte[] ba)
        {
            SHA256 mySHA256 = SHA256.Create();
            byte[] hashValue;
            hashValue = mySHA256.ComputeHash(ba);
            return ByteArrayToString(hashValue);
        }
        public  static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new(ba.Length * 2 + 2);
            hex.Append("0x");
            foreach (byte b in ba)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
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
                File.AppendAllText(logFilePath,logText+Environment.NewLine);
        }

    }
}
