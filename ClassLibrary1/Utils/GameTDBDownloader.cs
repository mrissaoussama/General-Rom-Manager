using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace RomManagerShared.Utils
{
    public class GameTDBDownloader
    {
        public static async Task DownloadAndExtractZip(string gameTDBurl, string savepath)
        {
            if (string.IsNullOrEmpty(gameTDBurl) || string.IsNullOrEmpty(savepath))
            {
                FileUtils.Log("GameTDB: path or url is invalid");
                return;
            }

            using (HttpClient httpClient = new())
            {
                // Download the ZIP file
                using Stream zipStream = await httpClient.GetStreamAsync(gameTDBurl);
                using FileStream fileStream = File.Create("temp.zip");
                await zipStream.CopyToAsync(fileStream);
            }

            // Extract the contents of the ZIP file
            ZipFile.ExtractToDirectory("temp.zip", Path.GetDirectoryName(savepath));

            // Delete the temporary ZIP file
            File.Delete("temp.zip");
        }
    }
}