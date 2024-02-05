using RomManagerShared.Base;
using System.Text.RegularExpressions;namespace RomManagerShared.Utils
{
    public class FileRenamer
    {
        public static void RenameFiles(IEnumerable<Rom> romList, string formatString)
        {
            if (romList is null)
            {
                Console.WriteLine("rom list null");
                return;
            }
            if (!romList.Any())
            {
                Console.WriteLine("No ROMs to rename.");
                return;
            }            var romGroups = romList.GroupBy(rom => rom.Path);
            foreach (var romGroup in romGroups)
            {
                var group = romGroup.ToList();
                RenameFilesInGroup(group, formatString);
            }
        }        private static void RenameFilesInGroup(IEnumerable<Rom> romGroup, string formatString)
        {
            if (!romGroup.Any())
            {
                return;
            }            string commonDirectory = Path.GetDirectoryName(romGroup.First().Path);
            string newFileName = GenerateFileName(romGroup, formatString);
            Console.WriteLine(newFileName);
            if (newFileName == Path.GetFileName(romGroup.First().Path))
            {
                return;
            }
            string newpath = "";
            try
            {
                var sourcePath = Path.Combine(commonDirectory, romGroup.First().Path);
                if (Path.GetFileName(sourcePath) != newFileName)
                    newpath = RenameFile(sourcePath, newFileName);
            }
            catch (Exception ex)
            {            }
            foreach (var rom in romGroup)
            {
                rom.Path = newpath;
            }
        }
        static string[] VersionPlaceholders = ["{Version}"];
        static string[] DLCPlaceholders = ["{DLCCount}"];

        private static string GenerateFileName(IEnumerable<Rom> romGroup, string formatString)
        {
            Rom romToRename = romGroup.FirstOrDefault(rom => rom is Game) ?? romGroup.First();
            string fileName = formatString;

            ReplacePlaceholder(ref fileName, "{TitleName}", romToRename.Titles?.FirstOrDefault()?.Value ?? "");
            ReplacePlaceholder(ref fileName, "{TitleID}", romToRename.TitleID ?? "");

            if (romToRename.Regions?.Count > 0 == true && romToRename.Regions.First() != Region.Unknown)
            {
                ReplacePlaceholders(ref fileName, ["[{Region}]", "{Region}"], string.Join(",", romToRename.Regions));
            }
            else
            {
                ReplacePlaceholders(ref fileName, ["[{Region}]", "{Region}"], "");
            }

            var version = GetVersion(romGroup) ?? "";
            var dlcCount = GetDLCCount(romGroup);
            if (romToRename is not Game || dlcCount == 0)
            {
                ReplacePlaceholders(ref fileName, ["[d{DLCCount}]", "[{DLCCount}]", "d{DLCCount}"], "");
            }
            else
            {
                ReplacePlaceholders(ref fileName, DLCPlaceholders, dlcCount.ToString());
            }

            if (version == "")
            {
                ReplacePlaceholders(ref fileName, ["[v{Version}]", "v{Version}", "[{Version}]"], "");
            }
            else
            {
                ReplacePlaceholders(ref fileName, VersionPlaceholders, version);
            }

            fileName += Path.GetExtension(romGroup.First().Path);
            return fileName;
        }

        private static void ReplacePlaceholder(ref string input, string placeholder, string replacement)
        {
            input = input.Replace(placeholder, replacement).Trim();
        }

        private static void ReplacePlaceholders(ref string input, string[] placeholderlist, string value)
        {
            foreach (var placeholder in placeholderlist)
            {
                ReplacePlaceholder(ref input, placeholder, value);
            }
        }
        private static string? GetVersion(IEnumerable<Rom> romGroup)
        {
            if (romGroup.Count() == 1)
            {
                return romGroup.First()?.Version?.ToString();
            }            foreach (var rom in romGroup)
            {
                if (rom is Update updateMetaData)
                {
                    return updateMetaData.Version?.ToString();
                }
            }
            return romGroup.First()?.Version?.ToString();
        }        private static int GetDLCCount(IEnumerable<Rom> romGroup)
        {
            return romGroup.Count(rom => rom is DLC);
        }
        private static string RenameFile(string sourcePath, string newFileName)
        {
            try
            {
                string directory = Path.GetDirectoryName(sourcePath);
                string extension = Path.GetExtension(newFileName);
                int count = 1;
                var invalidChars = Path.GetInvalidFileNameChars();
                string newFileNameCleaned = new string(newFileName
                    .Select(c => invalidChars.Contains(c) ? ' ' : c)
                    .ToArray());
                newFileNameCleaned = Regex.Replace(newFileNameCleaned, @"\s+", " ");
                string newFilePath = Path.Combine(directory, newFileNameCleaned);

                while (File.Exists(newFilePath) && sourcePath != newFileName)
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

    }
}
