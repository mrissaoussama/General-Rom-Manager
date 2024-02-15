using RomManagerShared.Switch.Configuration;
using RomManagerShared.WiiU.Configuration;

namespace RomManagerShared.Utils;

public class GithubDownloader
{    public static async Task DownloadSwitchTitleDBFiles(bool overrideExistingFiles = false)
    {
        var regionFiles = SwitchConfiguration.GetTitleDBRegionFiles();

        if (regionFiles != null)
        {
            foreach (var regionFile in regionFiles)
            {
                var path = Path.Combine(SwitchConfiguration.GetTitleDBPath(), regionFile);
                if (File.Exists(path) && overrideExistingFiles is false)
                {
                    Console.WriteLine("skipping " + path);
                    continue;
                }
                var regionUrl = SwitchConfiguration.GetTitleDBUrl() + regionFile;

                if (!string.IsNullOrEmpty(regionUrl))
                {
                    Console.WriteLine("downloading " + regionUrl);
                    await DownloadFile(regionUrl, path);
                }
            }
        }
        else
        {
            Console.WriteLine("No Switch title DB region files specified in the configuration.");
        }
    }    public async Task DownloadSwitchVersionsFile()
    {  string VersionsPath = SwitchConfiguration.GetVersionsPath();
    string VersionsUrl = SwitchConfiguration.GetVersionsUrl();

    await DownloadFile(VersionsUrl, VersionsPath);
    }    private static async Task DownloadFile(string fileUrl, string localFileName)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(300);
        try
        {
            var response = await httpClient.GetAsync(fileUrl);            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsByteArrayAsync();
                var localFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, localFileName);                if (File.Exists(localFilePath))
                {
                    var localFileSize = new FileInfo(localFilePath).Length;                    if (content.Length > localFileSize)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(localFilePath));
                        File.WriteAllBytes(localFilePath, content);
                        Console.WriteLine($"Updated {localFileName} file.");
                    }
                    else
                    {
                        Console.WriteLine($"Local {localFileName} file is up to date.");
                    }
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(localFilePath));
                    File.WriteAllBytes(localFilePath, content);
                    Console.WriteLine($"Downloaded {localFileName} file.");
                }
            }
            else
            {
                Console.WriteLine($"Failed to download {localFileName} file. Status Code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading {localFileName} file: {ex.Message}");
        }
    }

    public static async Task DownloadSwitchGlobalTitleDBFile(bool overrideExistingFile = false)
    {
        var path = Path.Combine(SwitchConfiguration.GetTitleDBPath(), SwitchConfiguration.GetGlobalTitleDBPath());
        if (!File.Exists(path) || overrideExistingFile is true)
        {
            await DownloadFile(SwitchConfiguration.GetGlobalTitleDBUrl(), path);
        }
    }
    public static async Task DownloadWiiUTitleDBFile(bool overrideExistingFile = false)
    {
        var path = WiiUConfiguration.GetTitleDBPath();
        if (!File.Exists(path) || overrideExistingFile is true)
        {
            await DownloadFile(WiiUConfiguration.GetTitleDBUrl(), path);
        }
    }
}
