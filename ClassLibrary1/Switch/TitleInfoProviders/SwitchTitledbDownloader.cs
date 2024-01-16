namespace RomManagerShared.Switch.TitleInfoProviders
{
    public class SwitchTitledbDownloader
    {
        private readonly string TitledbUrl =RomManagerConfiguration.GetSwitchTitleDBUrl();
        private readonly string VersionsUrl =RomManagerConfiguration.GetSwitchVersionsUrl();
        private readonly string VersionsPath =RomManagerConfiguration.GetSwitchVersionsPath();
        private readonly string TitledbPath = RomManagerConfiguration.GetSwitchTitleDBPath();

        public async Task DownloadTitledbFile()
        {
            await DownloadFile(TitledbUrl, TitledbPath);
        }

        public async Task DownloadVersionsFile()
        {
            await DownloadFile(VersionsUrl, VersionsPath);
        }

        private static async Task DownloadFile(string fileUrl, string localFileName)
        {
            using var httpClient = new HttpClient();
            try
            {
                var response = await httpClient.GetAsync(fileUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsByteArrayAsync();
                    var localFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, localFileName);

                    if (File.Exists(localFilePath))
                    {
                        var localFileSize = new FileInfo(localFilePath).Length;

                        if (content.Length > localFileSize)
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
    }
}
