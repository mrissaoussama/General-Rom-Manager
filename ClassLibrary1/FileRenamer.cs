using RomManagerShared;
using LibHac.Gc.Impl;
using RomManagerShared.Switch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace RomManagerShared.Switch
{
    public class FileRenamer
    {
        public static void RenameFiles(List<IRom> romList, string formatString)
        {
            if (romList is null)
            {
                Console.WriteLine("rom list null");
                return;
            }
            if (romList.Count == 0)
            {
                Console.WriteLine("No ROMs to rename.");
                return;
            }

            var romGroups = romList.GroupBy(rom => rom.Path);
            foreach (var romGroup in romGroups)
            {
                var group = romGroup.ToList();
                RenameFilesInGroup(group, formatString);
            }
        }

        private static void RenameFilesInGroup(List<IRom> romGroup, string formatString)
        {
            if (romGroup.Count == 0)
            {
                return;
            }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string commonDirectory = Path.GetDirectoryName(romGroup[0].Path);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            string newFileName = GenerateFileName(romGroup, formatString);
            Console.WriteLine(newFileName);
            if (newFileName == Path.GetFileName(romGroup[0].Path))
            { return;
            }
            string newpath = "";
            try
            {
                 newpath = RenameFile(Path.Combine(commonDirectory, romGroup[0].Path), newFileName);
            } 
            catch(Exception ex) {
            
            }
                foreach(var rom in romGroup)
            {
                rom.Path = newpath;
            }
        }

        private static string GenerateFileName(List<IRom> romGroup, string formatString)
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            IRom romToRename = null;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            if (romGroup.Count > 1)
            {
                foreach (var rom in romGroup)
                {
                    if (rom is IGame gameMetaData)
                    {
                        romToRename = rom;
                        break;
                    }
                    else romToRename = romGroup.First();

                }
            }
            else { romToRename = romGroup.First(); }

            string fileName = formatString;
            fileName = fileName.Replace("{TitleName}", romToRename!.TitleName).Trim();
            fileName = fileName.Replace("{TitleID}", romToRename.TitleID).Trim();
            var region = romToRename.Region;
            if (string.IsNullOrEmpty(region))
            {
                fileName = fileName.Replace("[{Region}]", "").Trim();
                fileName = fileName.Replace("{Region}", "").Trim();
            }
            else
            {
                fileName = fileName.Replace("{Region}", region).Trim();
            }
            var version = GetVersion(romGroup);
            if (romGroup.Count == 1 && (romGroup[0] is IDLC))
            {
                fileName = fileName.Replace("[v{Version}]", "").Trim();
                fileName = fileName.Replace("v{Version}", "").Trim();
                fileName = fileName.Replace("[{Version}]", "").Trim();
                fileName = fileName.Replace("{Version}", "").Trim();
            }
            else
            {
                fileName = fileName.Replace("{Version}", version).Trim();
            }
            var dlcCount = GetDLCCount(romGroup);
            if (romGroup.Count == 1 && (romGroup[0] is IDLC || romGroup[0] is IUpdate))
            {
                fileName = fileName.Replace("[d{DLCCount}]", "").Trim();
                fileName = fileName.Replace("d{DLCCount}", "").Trim();
                fileName = fileName.Replace("[{DLCCount}]", "").Trim();
                fileName = fileName.Replace("{DLCCount}", "").Trim();

            }
            else
            {
                fileName = fileName.Replace("{DLCCount}", dlcCount.ToString()).Trim();
            }
            fileName += Path.GetExtension(romGroup[0].Path);
            return fileName;
        }

        private static string GetVersion(List<IRom> romGroup)
        {
            if (romGroup.Count == 1)
            {
                return romGroup[0].Version.ToString();
            }

            foreach (var rom in romGroup)
            {
                if (rom is IUpdate updateMetaData)
                {
                    return updateMetaData.Version.ToString();
                }
            }

            // No UpdateMetadata found, use the version of the first ROM
            return romGroup[0].Version.ToString();
        }

        private static int GetDLCCount(List<IRom> romGroup)
        {
            int dlcCount = 0;

            foreach (var rom in romGroup)
            {
                if (rom is IDLC)
                {
                    dlcCount++;
                }
            }

            return dlcCount;
        }
        private static string RenameFile(string sourcePath, string newFileName)
        {
            try
            {
                string directory = Path.GetDirectoryName(sourcePath);
                 string extension = Path.GetExtension(newFileName);
                int count = 1;
                string originalNewFileName = newFileName;
                var invalidChars = Path.GetInvalidFileNameChars();
                string newFileNameCleaned = new string(newFileName
                    .Select(c => invalidChars.Contains(c) ? ' ' : c)
                    .ToArray());
                string newFilePath = Path.Combine(directory, newFileNameCleaned);
                int maxRetries = 5;
                while (File.Exists(newFilePath))
                {
                    newFileNameCleaned = $"{Path.GetFileNameWithoutExtension(newFileNameCleaned)} ({count}){extension}";
                    count++;
                    newFilePath = Path.Combine(directory, newFileNameCleaned);
                }
                File.Move(sourcePath, newFilePath);
                return newFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error renaming file: {ex.Message}");
                return sourcePath;
            }
        }


        private static bool IsFileInUse(Exception ex)
        {
            const int ERROR_SHARING_VIOLATION = 32;
            const int ERROR_LOCK_VIOLATION = 33;
            long hr = Marshal.GetHRForException(ex) & 0xFFFF;
            if ( (hr == ERROR_SHARING_VIOLATION || hr== ERROR_LOCK_VIOLATION))
            {
                return true; // File is in use
            }

            return false; // Other IO exception
        }



    }
}
