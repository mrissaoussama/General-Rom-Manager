using RomManagerShared.ThreeDS.Configuration;using RomManagerShared.Utils;namespace RomManagerShared.ThreeDS.TitleInfoProviders;

public class ThreeDSTitleDBDownloader
{
    private const string BaseUrl = "https://github.com/hax0kartik/3dsdb/raw/master/jsons/";
    private string[]? RegionFiles;
    public async Task DownloadRegionFiles()
    {
        RegionFiles = ThreeDSConfiguration.GetTitleDBRegionFilenames();
        if (RegionFiles == null)
        {
            FileUtils.Log("Region Files missing in config");
            return;
        }
        string? savePath = ThreeDSConfiguration.GetTitleDBPath();
        if (savePath == null)
        {
            FileUtils.Log("save path missing in config"); ;
            return;
        }
        foreach (var regionFile in RegionFiles)
        {
            if (File.Exists(savePath + regionFile))
                continue;
            string fileUrl = $"{BaseUrl}{regionFile}";
            string localFileName = savePath + regionFile;
            await DownloadFile(fileUrl, localFileName);
        }
    }
    private static async Task DownloadFile(string fileUrl, string localFileName)
    {
        using var httpClient = new HttpClient();
        try
        {
            var response = await httpClient.GetAsync(fileUrl);            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsByteArrayAsync();
                var localFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, localFileName);                if (File.Exists(localFilePath))
                {
                    var localFileSize = new FileInfo(localFilePath).Length;                    if (content.Length > localFileSize)
                    {
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
}
