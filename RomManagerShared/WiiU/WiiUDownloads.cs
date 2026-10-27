using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
using RomManagerShared.WiiU.Configuration;

namespace RomManagerShared.WiiU;

public sealed class WiiUDownloads : IDownloadableResources
{
    public IEnumerable<DownloadMethodInfo> GetMissingResources()
    {
        try
        {
            var path = WiiUConfiguration.GetTitleDBPath();
            if (!string.IsNullOrEmpty(path) && !File.Exists(path))
                return [new DownloadMethodInfo { DownloadName = "WiiU TitleDB", Url = [WiiUConfiguration.GetTitleDBUrl()], FilePaths = [path] }];
        }
        catch (Exception ex) { AppLog.Warn($"Wii U title database: {ex.Message}"); }
        return [];
    }
}
