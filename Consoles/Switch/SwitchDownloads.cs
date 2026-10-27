using RomManagerShared.Interfaces;
using RomManagerShared.Switch.Configuration;
using RomManagerShared.Utils;

namespace RomManagerShared.Switch;

/// <summary>blawar/titledb files: region title lists, the global list and versions.json.</summary>
public sealed class SwitchDownloads : IDownloadableResources
{
    public static async Task DownloadTitleDBFiles(bool overwrite = false)
    {
        foreach (var regionFile in SwitchConfiguration.GetTitleDBRegionFiles() ?? [])
            if (!string.IsNullOrEmpty(regionFile))
                await FileDownloader.DownloadIfMissing(SwitchConfiguration.GetTitleDBUrl() + regionFile,
                    Path.Combine(SwitchConfiguration.GetTitleDBPath(), regionFile), overwrite);
    }

    public static Task DownloadVersionsFile(bool overwrite = false) =>
        FileDownloader.DownloadIfMissing(SwitchConfiguration.GetVersionsUrl(), SwitchConfiguration.GetVersionsPath(), overwrite);

    public static Task DownloadGlobalTitleDBFile(bool overwrite = false) =>
        FileDownloader.DownloadIfMissing(SwitchConfiguration.GetGlobalTitleDBUrl(),
            Path.Combine(SwitchConfiguration.GetTitleDBPath(), SwitchConfiguration.GetGlobalTitleDBPath()), overwrite);

    public IEnumerable<DownloadMethodInfo> GetMissingResources()
    {
        var missing = new List<DownloadMethodInfo>();
        try
        {
            var titleDbPath = SwitchConfiguration.GetTitleDBPath();
            if (!string.IsNullOrEmpty(titleDbPath))
                foreach (var regionFile in SwitchConfiguration.GetTitleDBRegionFiles() ?? [])
                {
                    if (string.IsNullOrEmpty(regionFile)) continue;
                    var localPath = Path.Combine(titleDbPath, regionFile);
                    if (!File.Exists(localPath))
                        missing.Add(new DownloadMethodInfo
                        {
                            DownloadName = $"Switch titledb ({regionFile})",
                            Url = [SwitchConfiguration.GetTitleDBUrl() + regionFile],
                            FilePaths = [localPath],
                        });
                }

            var versionsPath = SwitchConfiguration.GetVersionsPath();
            if (!string.IsNullOrEmpty(versionsPath) && !File.Exists(versionsPath))
                missing.Add(new DownloadMethodInfo { DownloadName = "Switch versions", Url = [SwitchConfiguration.GetVersionsUrl()], FilePaths = [versionsPath] });

            var globalPath = Path.Combine(titleDbPath, SwitchConfiguration.GetGlobalTitleDBPath());
            if (!File.Exists(globalPath))
                missing.Add(new DownloadMethodInfo { DownloadName = "Switch global titledb", Url = [SwitchConfiguration.GetGlobalTitleDBUrl()], FilePaths = [globalPath] });
        }
        catch (Exception ex) { AppLog.Warn($"Switch title databases: {ex.Message}"); }
        return missing;
    }
}
