namespace RomManagerShared.Utils.PKGUtils
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text.Json;

    public class PSNPackageInfo
    {
        public string TitleId { get; set; }
        public string Title { get; set; }
        public string Region { get; set; }
        public float MinFirmware { get; set; }
        public float Version { get; set; }
        public string ContentId { get; set; }
        public string PkgPlatform { get; set; }
        public string PkgType { get; set; }

    }

    public class PSNPackageInfoFetcher
    {
        public static PSNPackageInfo FetchPackageInfo(string pkgFilePath)
        {
            // Launch Python script to get package info
            ProcessStartInfo psi = new()
            {
                FileName = "python",
                Arguments = $"Utils\\PKGUtils\\PSN_get_pkg_info.py \"{pkgFilePath}\" -f 3",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using Process process = Process.Start(psi);
            using StreamReader reader = process.StandardOutput;
            string output = reader.ReadToEnd();
            var packageInfoDto = DeserializePackageInfo(output);
            return packageInfoDto;
        }

        private static PSNPackageInfo DeserializePackageInfo(string jsonOutput)
        {if(string.IsNullOrEmpty(jsonOutput))
            {
                throw new Exception("invalid PKG");
            }
            using JsonDocument document = JsonDocument.Parse(jsonOutput);
            var root = document.RootElement;

            var results = root.GetProperty("results");

            PSNPackageInfo packageInfo = new()
            {
                TitleId = results.GetProperty("titleId").GetString(),
                Title = results.GetProperty("title").GetString(),
                Region = results.GetProperty("region").GetString(),
                MinFirmware = results.GetProperty("minFw").GetSingle(),
                Version = results.GetProperty("version").GetSingle(),
                ContentId = results.GetProperty("contentId").GetString(),
                PkgPlatform = results.GetProperty("pkgPlatform").GetString(),
                PkgType = results.GetProperty("pkgType").GetString()
            };

            return packageInfo;
        }

    }
}
