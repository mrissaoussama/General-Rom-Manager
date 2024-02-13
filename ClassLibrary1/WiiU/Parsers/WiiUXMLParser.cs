using LibHac.Tools.Fs;
using RomManagerShared.Base;
using RomManagerShared.Switch;
using RomManagerShared.Utils;
using RomManagerShared.WiiU;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

public class WiiUXmlParser : IRomParser
{
    public HashSet<string> Extensions { get; set; } = ["xml"];

    public WiiUXmlParser()
    {
    }

    public async Task<HashSet<Rom>> ProcessFile(string switchRomPath)
    {
        HashSet<Rom> list = [];
        string extension = Path.GetExtension(switchRomPath).TrimStart('.').ToLower();
        if (!Extensions.Contains(extension))
        {
            return new HashSet<Rom>();
        }
        if (Path.GetFileName(switchRomPath)!="app.xml")
        {
            return new HashSet<Rom>();
        }
        string DirectoryName = Path.GetDirectoryName(switchRomPath);

        Rom rom = new();
        string appXmlPath = Path.Combine(DirectoryName, "app.xml");
        string codedir = Path.GetDirectoryName(switchRomPath);
        string gamedir = Path.GetDirectoryName(codedir);

        if (File.Exists(appXmlPath))
        {
            // Read and parse app.xml file
            string appXmlContent = await File.ReadAllTextAsync(appXmlPath);
            rom = WiiUUtils.ParseAppXml(appXmlContent, rom);
        }

        string metaDirectory = Path.Combine(gamedir, "meta");
        if (Directory.Exists(metaDirectory))
        {
            // Check if meta.xml file exists
            string metaXmlPath = Path.Combine(metaDirectory, "meta.xml");
            if (File.Exists(metaXmlPath))
            {
                string metaXmlContent = await File.ReadAllTextAsync(metaXmlPath);
               rom= WiiUUtils. ParseMetaXml(metaXmlContent, rom);
            }

            List<string> images = new List<string>();
            var iconTexpath = Path.Combine(metaDirectory, "iconTex.tga");
            if (File.Exists(iconTexpath))
                images.Add(iconTexpath);
            var bootTvTexpath = Path.Combine(metaDirectory, "bootTvTex.tga");
            if (File.Exists(bootTvTexpath))
                images.Add(bootTvTexpath);
            var bootDrcTexpath = Path.Combine(metaDirectory, "bootDrcTex.tga");
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
        rom.IsFolderFormat = true;
        list.Add(rom);
        return list;
    }

  
}
