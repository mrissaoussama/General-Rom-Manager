using RomManagerShared.Switch.Configuration;
using RomManagerShared.ThreeDS.Configuration;
using RomManagerShared.WiiU.Configuration;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace RomManagerShared.Utils;
[AttributeUsage(AttributeTargets.Method)]
public class ResourceDownloadAttribute : Attribute
{
    public string ResourceName { get; }
    public string ResourceURLsMethodNames { get; }
    public string ResourcePathsMethodNames { get; }
    public Type ConfigurationTypes { get; }
    public ResourceDownloadAttribute(string resourceName, string  resourceURLs, string  resourcePaths, Type configurationType)
    {
        ResourceName = resourceName;
        ResourceURLsMethodNames = resourceURLs;
        ResourcePathsMethodNames = resourcePaths;
        ConfigurationTypes = configurationType;
    }
}
public class DownloadMethodInfo
{public string DownloadName { get; set; }
    public string[] Url { get; set; }
    public string[] FilePaths{ get; set; }
}
public class FileDownloader
{
    public static DownloadMethodInfo[] GetResourceDownloads()
    {
        List<DownloadMethodInfo> list = [];
        MethodInfo[] methods = typeof(FileDownloader).GetMethods(BindingFlags.Public | BindingFlags.Static);
        foreach (MethodInfo method in methods)
        {
            ResourceDownloadAttribute attribute = method.GetCustomAttribute<ResourceDownloadAttribute>();
            if (attribute != null)
            {
                DownloadMethodInfo info = new();
                info.DownloadName = attribute.ResourceName;
                MethodInfo getUrlMethod = attribute.ConfigurationTypes.GetMethod(attribute.ResourceURLsMethodNames);
                MethodInfo getPathMethod = attribute.ConfigurationTypes.GetMethod(attribute.ResourcePathsMethodNames);

                object urlObject = getUrlMethod.Invoke(null, null);
                object pathObject = getPathMethod.Invoke(null, null);

                if (urlObject is string[])
                {
                    string[] resourceURLs = (string[])urlObject;
                    info.Url = resourceURLs;
                }
                else if (urlObject is string)
                {
                    string urlString = (string)urlObject;
                    info.Url = [urlString];

                }

                if (pathObject is string[])
                {
                    string[] resourcePaths = (string[])pathObject;
                    info.FilePaths = resourcePaths;
                }
                else if (pathObject is string)
                {
                    string pathString = (string)pathObject;
                    info.FilePaths = [pathString];
                }
                list.Add(info);
            }
        }
        return list.ToArray();

    }

    /// <summary>
    /// Returns one <see cref="DownloadMethodInfo"/> per file that is missing on disk.
    /// Each returned entry has exactly one URL and one FilePath so callers can
    /// iterate and queue individual downloads.
    /// </summary>
    public static DownloadMethodInfo[] GetMissingResources()
    {
        var missing = new List<DownloadMethodInfo>();

        // ── Switch region title-DB files ──────────────────────────────────
        try
        {
            var regionFiles = SwitchConfiguration.GetTitleDBRegionFiles();
            var titleDbPath = SwitchConfiguration.GetTitleDBPath();
            var baseUrl     = SwitchConfiguration.GetTitleDBUrl();

            if (regionFiles != null && !string.IsNullOrEmpty(titleDbPath))
            {
                foreach (var regionFile in regionFiles)
                {
                    if (string.IsNullOrEmpty(regionFile)) continue;
                    var localPath = Path.Combine(titleDbPath, regionFile);
                    if (!File.Exists(localPath))
                        missing.Add(new DownloadMethodInfo
                        {
                            DownloadName = $"Switch titledb ({regionFile})",
                            Url          = [baseUrl + regionFile],
                            FilePaths    = [localPath],
                        });
                }
            }
        }
        catch (Exception ex) { FileUtils.Log($"GetMissingResources (Switch region): {ex.Message}"); }

        // ── Switch versions.json ──────────────────────────────────────────
        try
        {
            var versionsPath = SwitchConfiguration.GetVersionsPath();
            var versionsUrl  = SwitchConfiguration.GetVersionsUrl();
            if (!string.IsNullOrEmpty(versionsPath) && !File.Exists(versionsPath))
                missing.Add(new DownloadMethodInfo
                {
                    DownloadName = "Switch versions",
                    Url          = [versionsUrl],
                    FilePaths    = [versionsPath],
                });
        }
        catch (Exception ex) { FileUtils.Log($"GetMissingResources (Switch versions): {ex.Message}"); }

        // ── Switch global titledb ─────────────────────────────────────────
        try
        {
            var globalPath = Path.Combine(SwitchConfiguration.GetTitleDBPath(),
                                          SwitchConfiguration.GetGlobalTitleDBPath());
            var globalUrl = SwitchConfiguration.GetGlobalTitleDBUrl();
            if (!string.IsNullOrEmpty(globalPath) && !File.Exists(globalPath))
                missing.Add(new DownloadMethodInfo
                {
                    DownloadName = "Switch global titledb",
                    Url          = [globalUrl],
                    FilePaths    = [globalPath],
                });
        }
        catch (Exception ex) { FileUtils.Log($"GetMissingResources (Switch global): {ex.Message}"); }

        // ── WiiU title-DB ─────────────────────────────────────────────────
        try
        {
            var wiiuPath = WiiUConfiguration.GetTitleDBPath();
            var wiiuUrl  = WiiUConfiguration.GetTitleDBUrl();
            if (!string.IsNullOrEmpty(wiiuPath) && !File.Exists(wiiuPath))
                missing.Add(new DownloadMethodInfo
                {
                    DownloadName = "WiiU TitleDB",
                    Url          = [wiiuUrl],
                    FilePaths    = [wiiuPath],
                });
        }
        catch (Exception ex) { FileUtils.Log($"GetMissingResources (WiiU): {ex.Message}"); }

        // ── 3DS region files ──────────────────────────────────────────────
        try
        {
            var regionFiles = ThreeDSConfiguration.GetTitleDBRegionFilenames();
            var savePath    = ThreeDSConfiguration.GetTitleDBPath();
            var baseUrl     = ThreeDSConfiguration.GetTitleDBBaseUrl();

            if (regionFiles != null && !string.IsNullOrEmpty(savePath))
            {
                foreach (var regionFile in regionFiles)
                {
                    if (string.IsNullOrEmpty(regionFile)) continue;
                    var localPath = savePath + regionFile;
                    if (!File.Exists(localPath))
                        missing.Add(new DownloadMethodInfo
                        {
                            DownloadName = $"3DS titledb ({regionFile})",
                            Url          = [baseUrl + regionFile],
                            FilePaths    = [localPath],
                        });
                }
            }
        }
        catch (Exception ex) { FileUtils.Log($"GetMissingResources (3DS): {ex.Message}"); }

        return missing.ToArray();
    }

    private static readonly HttpClient _sharedClient = new() { Timeout = System.Threading.Timeout.InfiniteTimeSpan };

    public static async Task StartFileDownload(string fileUrl, string localFileName, string? extractPath = null)
    {
        try
        {
            var dir = Path.GetDirectoryName(localFileName);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using var response = await _sharedClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var file   = File.Create(localFileName);
            await stream.CopyToAsync(file);

            if (extractPath is not null && localFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(extractPath);
                ZipFile.ExtractToDirectory(localFileName, extractPath, overwriteFiles: true);
            }

            FileUtils.Log($"Downloaded: {localFileName}");
        }
        catch (Exception ex)
        {
            FileUtils.Log($"Error downloading {fileUrl}: {ex.Message}");
        }
    }
    [ResourceDownload("Switch region titledb",  nameof(SwitchConfiguration.GetTitleDBUrl) ,  nameof(SwitchConfiguration.GetTitleDBRegionFiles) , typeof(SwitchConfiguration) )]
    public static async Task DownloadSwitchTitleDBFiles(bool overrideExistingFiles = false)
    {
        var regionFiles = SwitchConfiguration.GetTitleDBRegionFiles();

        if (regionFiles != null)
        {
            foreach (var regionFile in regionFiles)
            {
                var path = Path.Combine(SwitchConfiguration.GetTitleDBPath(), regionFile);
                if (File.Exists(path) && !overrideExistingFiles)
                {
                    FileUtils.Log("skipping " + path);
                    continue;
                }
                var regionUrl = SwitchConfiguration.GetTitleDBUrl() + regionFile;

                if (!string.IsNullOrEmpty(regionUrl))
                {
                    FileUtils.Log("downloading " + regionUrl);
                    await FileDownloader.StartFileDownload(regionUrl, path);

                }
            }
        }
        else
        {
            FileUtils.Log("No Switch title DB region files specified in the configuration.");
        }
    }
    public static async Task DownloadThreeDSRegionFiles(bool overrideFiles = false)
    {
        var BaseUrl = ThreeDSConfiguration.GetTitleDBBaseUrl();
        var RegionFiles = ThreeDSConfiguration.GetTitleDBRegionFilenames();
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
            if (File.Exists(savePath + regionFile) && overrideFiles is false)
                continue;
            string fileUrl = $"{BaseUrl}{regionFile}";
            string localFileName = savePath + regionFile;
            await StartFileDownload(fileUrl, localFileName);
        }
    }

    [ResourceDownload("Switch versions", nameof(SwitchConfiguration.GetVersionsUrl), nameof(SwitchConfiguration.GetVersionsPath), typeof(SwitchConfiguration))]

    public static async Task DownloadSwitchVersionsFile(bool overrideExistingFiles = false)
    {
        string VersionsPath = SwitchConfiguration.GetVersionsPath();
        string VersionsUrl = SwitchConfiguration.GetVersionsUrl();
        if (Path.Exists(VersionsPath) && overrideExistingFiles is false)
            return;
        await FileDownloader.StartFileDownload(VersionsUrl, VersionsPath);
    }
    [ResourceDownload("Switch titledb", nameof(SwitchConfiguration.GetGlobalTitleDBUrl), nameof(SwitchConfiguration.GetGlobalTitleDBPath), 
        typeof(SwitchConfiguration))]

    public static async Task DownloadSwitchGlobalTitleDBFile(bool overrideExistingFile = false)
    {
        var path = Path.Combine(SwitchConfiguration.GetTitleDBPath(), SwitchConfiguration.GetGlobalTitleDBPath());
        if (Path.Exists(path) && overrideExistingFile is false)
            return;
        await FileDownloader.StartFileDownload(SwitchConfiguration.GetGlobalTitleDBUrl(), path);
    }
    [ResourceDownload("WiiU TitleDB jsons", nameof(WiiUConfiguration.GetTitleDBUrl), nameof(WiiUConfiguration.GetTitleDBPath),
        typeof(WiiUConfiguration))]
    public static async Task DownloadWiiUTitleDBFile(bool overrideExistingFile = false)
    {
        var path = WiiUConfiguration.GetTitleDBPath();
        if (Path.Exists(path) && overrideExistingFile is false)
            return;
        await FileDownloader.StartFileDownload(WiiUConfiguration.GetTitleDBUrl(), path);
    }
    public async static Task<string> DownloadHtmlContent(string url)
    {
        try
        {
            using HttpClient client = new();
            string html = await client.GetStringAsync(url);
            return html;
        }
        catch (Exception ex)
        {
           FileUtils.Log($"Error downloading HTML content: {ex.Message}");
        }
        return await Task.FromResult(string.Empty);
    }
}
