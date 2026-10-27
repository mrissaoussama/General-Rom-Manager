using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace RomManagerShared.Utils;
/// <summary>One file a console module needs (title database, version list) and where it comes from.</summary>
public class DownloadMethodInfo
{
    public string DownloadName { get; set; } = "";
    public string[] Url { get; set; } = [];
    public string[] FilePaths { get; set; } = [];
}

/// <summary>Generic HTTP downloads. Console modules say what they need through <c>IDownloadableResources</c>.</summary>
public class FileDownloader
{
    private static readonly HttpClient _sharedClient = new() { Timeout = System.Threading.Timeout.InfiniteTimeSpan };

    /// <summary>Downloads <paramref name="fileUrl"/> unless <paramref name="localFileName"/> exists (or <paramref name="overwrite"/>).</summary>
    public static Task DownloadIfMissing(string fileUrl, string localFileName, bool overwrite = false) =>
        File.Exists(localFileName) && !overwrite ? Task.CompletedTask : StartFileDownload(fileUrl, localFileName);
    public static async Task StartFileDownload(string fileUrl, string localFileName, string? extractPath = null)
    {
        try
        {
            var dir = Path.GetDirectoryName(localFileName);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using var response = await _sharedClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using (var stream = await response.Content.ReadAsStreamAsync())
            await using (var file = File.Create(localFileName))
                await stream.CopyToAsync(file);

            if (extractPath is not null && localFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(extractPath);
                ZipFile.ExtractToDirectory(localFileName, extractPath, overwriteFiles: true);
            }

            AppLog.Info($"Downloaded: {localFileName}");
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Error downloading {fileUrl}: {ex.Message}");
        }
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
           AppLog.Warn($"Error downloading HTML content: {ex.Message}");
        }
        return await Task.FromResult(string.Empty);
    }
}
