// File: RomManagerShared/Organizers/PSVitaRomOrganizer.cs

using LibHac.Tools.Fs;
using RomManagerShared.Base;
using RomManagerShared.Configuration;
using RomManagerShared.Interfaces;
using RomManagerShared.PSVita;
using RomManagerShared.PSVita.Configuration;
using RomManagerShared.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RomManagerShared.Organizers;

public class PSVitaRomOrganizer : IRomOrganizer<PSVitaConsole>, ILicenseOrganizer<PSVitaConsole>
{
    #region Constants
    private const string LicenseFileName = "6488b73b912a753a492e2714e9b38bc7.rif";
    private static readonly string[] RequiredDirs = { "app", "sce_sys" };
    #endregion

    public string Description { get; set; } = "Organizes PS Vita ROMs into folders: app, patch, addcont, and handles licenses.";

    public void Organize(List<Rom> romList, List<List<Rom>> groupedRomList, bool organizeGamesOnly = false)
    {

        //remove non folder roms from romlist and groupedromlist
        romList = romList.Where(rom => rom.IsFolderFormat).ToList();
        groupedRomList = groupedRomList.Select(g => g.Where(rom => rom.IsFolderFormat).ToList()).ToList();
        if (romList == null || romList.Count == 0)
        {
            Console.WriteLine("No ROMs to organize.");
            return;
        }

        // Convert IGrouping to List<Rom>
        var romGroups = romList
            .Where(rom => rom.IsFolderFormat)
            .GroupBy(rom => GetBaseTitleId(rom.TitleID))
            .Select(g => g.ToList())
            .ToList();

        foreach (var group in romGroups)
        {
            OrganizeGroup(group, groupedRomList);
        }
    }

    private void OrganizeGroup(List<Rom> group, List<List<Rom>> groupedRomList)
    {
        var (games, updates, dlcs) = (
            group.OfType<PSVitaGame>().ToList(),
            group.OfType<PSVitaUpdate>().ToList(),
            group.OfType<PSVitaDLC>().ToList()
        );

        var baseTitleId = GetBaseTitleId(group.FirstOrDefault()?.TitleID);

        // Create target directory structure
        var targetRoot = CreateTargetDirectory(baseTitleId, games, updates, dlcs);
        var dirStructure = new VitaDirectoryStructure(targetRoot);

        try
        {
            // Process main content
            ProcessGames(games, dirStructure.App, groupedRomList);
            ProcessUpdates(updates, dirStructure.Patch, groupedRomList);
            ProcessDLCs(dlcs, dirStructure.Addcont, groupedRomList);

            // Handle nested content
            ProcessNestedContent(group, targetRoot, baseTitleId, groupedRomList);

            if (!dirStructure.Validate())
            {
                throw new InvalidOperationException("Final structure validation failed");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Organization failed: {ex.Message}");
            dirStructure.Dispose();
            throw;
        }
    }

    #region Processing Methods
    private void ProcessGames(List<PSVitaGame> games, string appDirectory, List<List<Rom>> groupedRomList)
    {
        foreach (var game in games)
        {
            if (IsNestedContent(game, groupedRomList)) continue;
            MoveRomContent(game, appDirectory, groupedRomList);
        }
    }

    private void ProcessUpdates(List<PSVitaUpdate> updates, string patchDirectory, List<List<Rom>> groupedRomList)
    {
        var latestUpdate = updates.OrderByDescending(u => u.Version).FirstOrDefault();
        if (latestUpdate != null)
        {
            MoveRomContent(latestUpdate, patchDirectory, groupedRomList);
        }
    }

    private void ProcessDLCs(List<PSVitaDLC> dlcs, string addcontDirectory, List<List<Rom>> groupedRomList)
    {
        foreach (var dlc in dlcs)
        {
            MoveRomContent(dlc, addcontDirectory, groupedRomList);
        }
    }
    #endregion

    #region Nested Content Handling
    private bool IsNestedContent(Rom rom, List<List<Rom>> allRoms)
    {
        return allRoms.SelectMany(g => g)
            .Any(other => rom != other &&
                 rom.Path.StartsWith(other.Path, StringComparison.OrdinalIgnoreCase));
    }

    private void ProcessNestedContent(List<Rom> group, string targetRoot, string baseTitleId, List<List<Rom>> groupedRomList)
    {
        foreach (var rom in group)
        {
            var nestedDirs = Directory.GetDirectories(rom.Path, "*", SearchOption.AllDirectories)
                .Where(d => PSVitaUtils.IsPSVitaRomFolder(d));

            foreach (var nestedDir in nestedDirs)
            {
                var nestedTitleId = PSVitaUtils.GetBaseTitleIdFromFolder(nestedDir);
                var isRelated = nestedTitleId == baseTitleId;
                MoveNestedContent(nestedDir, targetRoot, isRelated, groupedRomList);
            }
        }
    }
    private void MoveNestedContent(string nestedDir, string targetRoot, bool isRelated, List<List<Rom>> groupedRomList)
    {
        try
        {
            string newPath;
            string parentDir = Directory.GetParent(nestedDir)?.FullName;

            if (isRelated)
            {
                // Maintain original directory structure relative to parent ROM
                string relativePath = Path.GetRelativePath(parentDir, nestedDir);
                newPath = Path.Combine(targetRoot, relativePath);
            }
            else
            {
                // Move to unrelated directory
                string unrelatedBase = Path.Combine(targetRoot, "Unrelated");
                newPath = Path.Combine(unrelatedBase, Path.GetFileName(nestedDir));
                Directory.CreateDirectory(unrelatedBase);
            }

            // Perform the actual directory move
            FileUtils.MoveDirectoryCrossPlatform(nestedDir, newPath);

            // Update all ROM entries with the new path
            UpdateRomPaths(nestedDir, newPath, groupedRomList);

            Console.WriteLine($"Moved nested content: {Path.GetFileName(nestedDir)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to move nested content {nestedDir}: {ex.Message}");
            throw;
        }
    }
    #endregion

    #region Directory Structure
    private class VitaDirectoryStructure : IDisposable
    {
        public string Root { get; }
        public string App => Path.Combine(Root, "app");
        public string Patch => Path.Combine(Root, "patch");
        public string Addcont => Path.Combine(Root, "addcont");

        public VitaDirectoryStructure(string rootPath)
        {
            Root = rootPath;
            Directory.CreateDirectory(App);
            Directory.CreateDirectory(Patch);
            Directory.CreateDirectory(Addcont);
        }

        public bool Validate() => RequiredDirs.All(d => Directory.Exists(Path.Combine(Root, d)));

        public void Dispose()
        {
            if (!Validate()) Directory.Delete(Root, true);
        }
    }
    #endregion

    #region Utility Methods
    private string GetBaseTitleId(string titleId)
    {
        return string.IsNullOrEmpty(titleId) ? titleId :
            titleId.Length >= 9 ? titleId[..9] : titleId;
    }

    private string CreateTargetDirectory(string baseTitleId,
        List<PSVitaGame> games, List<PSVitaUpdate> updates, List<PSVitaDLC> dlcs)
    {
        var basePath = PSVitaConfiguration.GetRomPath();
        var folderName = BuildFolderName(games.FirstOrDefault(), updates, dlcs);
        var cleanName = RomUtils.GetCleanName(folderName);
        var targetRoot = Path.Combine(basePath, cleanName);

        for (var i = 1; Directory.Exists(targetRoot); i++)
        {
            if (new VitaDirectoryStructure(targetRoot).Validate()) return targetRoot;
            targetRoot = Path.Combine(basePath, $"{cleanName} ({i})");
        }

        Directory.CreateDirectory(targetRoot);
        return targetRoot;
    }

    private string BuildFolderName(PSVitaGame game, List<PSVitaUpdate> updates, List<PSVitaDLC> dlcs)
    {
        var title = game?.Titles.FirstOrDefault()?.Value ?? "Unknown";
        var version = updates.OrderByDescending(u => u.Version)
            .FirstOrDefault()?.Version ?? game?.Version ?? "1.00";

        return $"{title} [{game?.TitleID}] [{version}]" +
            (dlcs.Count > 0 ? $" [{dlcs.Count} DLC]" : "");
    }

    private void MoveRomContent(Rom rom, string destination, List<List<Rom>> groupedRomList)
    {
        if (rom?.Path == null || !Directory.Exists(rom.Path)) return;

        try
        {
            FileUtils.MoveDirectoryCrossPlatform(rom.Path, destination);
            UpdateRomPaths(rom.Path, destination, groupedRomList);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error moving {rom.TitleID}: {ex.Message}");
            throw;
        }
    }

    private void UpdateRomPaths(string originalPath, string newPath, List<List<Rom>> groupedRomList)
    {
        foreach (var group in groupedRomList)
        {
            foreach (var rom in group.Where(r => r.Path == originalPath))
            {
                rom.Path = newPath;
            }
        }
    }
    #endregion

    #region License Organization
    public void OrganizeLicenses(List<License> licenseList, List<Rom> romList)
    {
        if (licenseList == null || licenseList.Count == 0)
        {
            Console.WriteLine("No licenses to organize.");
            return;
        }

        foreach (var license in licenseList.OfType<PSVitaLicense>())
        {
            ProcessLicense(license, romList);
        }
    }

    private void ProcessLicense(PSVitaLicense license, List<Rom> romList)
    {
        var gameRom = romList.FirstOrDefault(r =>
            GetBaseTitleId(r.TitleID) == GetBaseTitleId(license.TitleID));

        if (gameRom?.Path == null)
        {
            Console.WriteLine($"No matching game for license {license.TitleID}");
            return;
        }

        var licensePath = Path.Combine(
            Path.GetDirectoryName(gameRom.Path),
            "license",
            license.LicenseId == "0000000000000000" ? "app" : "addcont",
            GetBaseTitleId(license.TitleID),
            license.LicenseId
        );

        try
        {
            Directory.CreateDirectory(licensePath);
            var licenseFile = Path.Combine(licensePath, LicenseFileName);

            if (!File.Exists(licenseFile))
            {
                File.Copy(license.Path, licenseFile);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error organizing license: {ex.Message}");
        }
    }
    #endregion
}