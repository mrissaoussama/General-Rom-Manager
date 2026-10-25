using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
using RomManagerShared.WiiU.Configuration;
using System.Diagnostics;

namespace RomManagerShared.WiiU.Parsers;

public class WiiUTMDTIKParser : IRomParser<WiiUConsole>
{
    public List<string> Extensions { get; set; }

    public WiiUTMDTIKParser()
    {
        Extensions = ["tmd"];
    }

    public async Task<List<Rom>> ProcessFile(string path)
    {
        List<Rom> list = [];
        string extension = Path.GetExtension(path)?.TrimStart('.').ToLower();

        if (!Extensions.Contains(extension))
        {
            return list;
        }

        string directory = Path.GetDirectoryName(path);

        string titleTikPath = Path.Combine(directory, $"title.tik");
        string titleTmdPath = Path.Combine(directory, $"title.tmd");

        if (!File.Exists(titleTikPath) || !File.Exists(titleTmdPath))
        {
            return list;
        }

        string codeAppXmlPath = Path.Combine(directory, "code", "app.xml");
        string metaMetaXmlPath = Path.Combine(directory, "meta", "meta.xml");
        var metadir = Path.GetDirectoryName(metaMetaXmlPath)!;
        var appdir = Path.GetDirectoryName(codeAppXmlPath)!;

        // cdecrypt writes into the ROM folder; only run it when enabled and only remove what it created.
        bool metaExisted = Directory.Exists(metadir);
        bool appExisted = Directory.Exists(appdir);
        bool decrypted = false;
        if (!(metaExisted && appExisted))
        {
            if (!WiiUConfiguration.AllowCdecrypt())
                return list;
            ProcessStartInfo psi = new()
            {
                FileName = Path.Combine(AppContext.BaseDirectory, "WiiU", "Parsers", "cdecrypt.exe"),
                Arguments = $"\"{titleTmdPath}\"",
                WorkingDirectory = directory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using Process process = Process.Start(psi)!;
            await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            decrypted = true;
        }
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
            List<string> images = [];
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
        if (decrypted)
        {
            if (!metaExisted && Directory.Exists(metadir))
                Directory.Delete(metadir, true);
            if (!appExisted && Directory.Exists(appdir))
                Directory.Delete(appdir, true);
        }
        rom.IsFolderFormat = true;
        rom.Path = path;
        list.Add(rom);
        return list;
    }
 
}
