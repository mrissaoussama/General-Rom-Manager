using RomManagerShared.Base;
using RomManagerShared.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using System.Threading.Tasks;

namespace RomManagerShared.WiiU.Parsers
{
    public class WiiUTMDTIKParser : IRomParser
    {
        public HashSet<string> Extensions { get; set; }

        public WiiUTMDTIKParser()
        {
            Extensions = new HashSet<string> { "tmd" };
        }

        public async Task<HashSet<Rom>> ProcessFile(string switchRomPath)
        {
            HashSet<Rom> list = new HashSet<Rom>();
            string extension = Path.GetExtension(switchRomPath)?.TrimStart('.').ToLower();

            if (!Extensions.Contains(extension))
            {
                return list;
            }

            string directory = Path.GetDirectoryName(switchRomPath);

            string titleTikPath = Path.Combine(directory, $"title.tik");
            string titleTmdPath = Path.Combine(directory, $"title.tik");

            if (!File.Exists(titleTikPath) || !File.Exists(titleTmdPath))
            {
                return list;
            }

            // Run the command-line tool "cdecrypt" with the path to the TMD file
            string cdecryptPath = "cdecrypt"; // Update with the actual path to cdecrypt
            string arguments = $"\"{titleTmdPath}\"";
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "WiiU\\Parsers\\cdecrypt.exe";
            psi.Arguments = arguments;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.CreateNoWindow = false;

            using Process process = Process.Start(psi);
            using StreamReader reader = process.StandardOutput;
            string output = reader.ReadToEnd();
            string codeAppXmlPath = Path.Combine(directory, "code", "app.xml");
            string metaMetaXmlPath = Path.Combine(directory, "meta", "meta.xml");
            var metadir = Path.GetDirectoryName(metaMetaXmlPath);
            var appdir= Path.GetDirectoryName(codeAppXmlPath);
            Rom rom = new();

            if (File.Exists(codeAppXmlPath) )
            {
                string appXmlContent = await File.ReadAllTextAsync(codeAppXmlPath);
                rom = WiiUUtils.ParseAppXml(appXmlContent, rom);
            }

            if (  File.Exists(metaMetaXmlPath))
            {
                string metaXmlContent = await File.ReadAllTextAsync(metaMetaXmlPath);
                rom = WiiUUtils.ParseMetaXml(metaXmlContent, rom);
                List<string> images = new List<string>();
                var iconTexpath = Path.Combine(metadir, "iconTex.tga");
                if (File.Exists(iconTexpath))
                    images.Add(iconTexpath);
                var bootTvTexpath = Path.Combine(metadir, "bootTvTex.tga");
                if (File.Exists(bootTvTexpath))
                    images.Add(bootTvTexpath);
                var bootDrcTexpath = Path.Combine(metadir, "bootDrcTex.tga");
                if (File.Exists(bootDrcTexpath))
                    images.Add(bootDrcTexpath);
                rom.AddImages(images);
            }
            var metadataClass = WiiUUtils.GetRomMetadataClass(rom.TitleID);
            if (metadataClass != rom.GetType())
            {
                var metadataInstance = Activator.CreateInstance(metadataClass);
                RomUtils.CopyNonNullProperties(rom, (Rom)metadataInstance);
                rom = (Rom)metadataInstance;
            }
            if (Directory.Exists(metadir))
                Directory.Delete(metadir, true);
            if (Directory.Exists(appdir))
                Directory.Delete(appdir, true);
            rom.IsFolderFormat = true;

            list.Add(rom);
            return list;
        }
     
    }
}
