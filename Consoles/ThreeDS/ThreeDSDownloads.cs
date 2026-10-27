using RomManagerShared.Interfaces;
using RomManagerShared.ThreeDS.Configuration;
using RomManagerShared.Utils;

namespace RomManagerShared.ThreeDS;

public sealed class ThreeDSDownloads : IDownloadableResources
{
    public static async Task DownloadRegionFiles(bool overwrite = false)
    {
        var savePath = ThreeDSConfiguration.GetTitleDBPath();
        if (string.IsNullOrEmpty(savePath)) return;
        foreach (var regionFile in ThreeDSConfiguration.GetTitleDBRegionFilenames() ?? [])
            await FileDownloader.DownloadIfMissing($"{ThreeDSConfiguration.GetTitleDBBaseUrl()}{regionFile}", savePath + regionFile, overwrite);
    }

    public IEnumerable<DownloadMethodInfo> GetMissingResources()
    {
        var missing = new List<DownloadMethodInfo>();
        try
        {
            var savePath = ThreeDSConfiguration.GetTitleDBPath();
            if (!string.IsNullOrEmpty(savePath))
                foreach (var regionFile in ThreeDSConfiguration.GetTitleDBRegionFilenames() ?? [])
                {
                    if (string.IsNullOrEmpty(regionFile) || File.Exists(savePath + regionFile)) continue;
                    missing.Add(new DownloadMethodInfo
                    {
                        DownloadName = $"3DS titledb ({regionFile})",
                        Url = [ThreeDSConfiguration.GetTitleDBBaseUrl() + regionFile],
                        FilePaths = [savePath + regionFile],
                    });
                }
        }
        catch (Exception ex) { AppLog.Warn($"3DS title databases: {ex.Message}"); }
        return missing;
    }
}
