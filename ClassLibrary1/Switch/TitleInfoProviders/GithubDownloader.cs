﻿namespace RomManagerShared.Switch.TitleInfoProviders
{
    public class GithubDownloader
    {
        private readonly string TitleDBUrl = RomManagerConfiguration.GetSwitchTitleDBUrl();
        private readonly string VersionsUrl = RomManagerConfiguration.GetSwitchVersionsUrl();
        private readonly string VersionsPath = RomManagerConfiguration.GetSwitchVersionsPath();
        private readonly string TitleDBPath = RomManagerConfiguration.GetSwitchTitleDBPath();
        {
            var regionFiles = RomManagerConfiguration.GetSwitchTitleDBRegionFiles();

            if (regionFiles != null)
            {
                foreach (var regionFile in regionFiles)
                {
                    var path = Path.Combine(RomManagerConfiguration.GetSwitchTitleDBPath(), regionFile);
                    if (File.Exists(path) && overrideExistingFiles is false)
                    {
                        Console.WriteLine("skipping " + path);
                        continue;
                    }
                    var regionUrl = RomManagerConfiguration.GetSwitchTitleDBUrl() + regionFile;

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
        }
        {
            await DownloadFile(VersionsUrl, VersionsPath);
        }
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(300);
            try
            {
                var response = await httpClient.GetAsync(fileUrl);
                {
                    var content = await response.Content.ReadAsByteArrayAsync();
                    var localFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, localFileName);
                    {
                        var localFileSize = new FileInfo(localFilePath).Length;
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

        public async Task DownloadSwitchGlobalTitleDBFile(bool overrideExistingFile = false)
        {
            var path = Path.Combine(RomManagerConfiguration.GetSwitchTitleDBPath(), RomManagerConfiguration.GetSwitchGlobalTitleDBPath());
            if (!File.Exists(path) || overrideExistingFile is true)
            {
                await DownloadFile(RomManagerConfiguration.GetSwitchGlobalTitleDBUrl(), path);
            }
        }
    }
}