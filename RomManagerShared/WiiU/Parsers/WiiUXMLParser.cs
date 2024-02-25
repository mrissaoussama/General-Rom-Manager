using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
namespace RomManagerShared.WiiU;
public class WiiUXmlParser : IRomParser<WiiUConsole>
{
    public List<string> Extensions { get; set; } = ["xml"];

    public WiiUXmlParser()
    {
    }

    public async Task<List<Rom>> ProcessFile(string path)
    {
        List<Rom> list = [];
        string extension = Path.GetExtension(path).TrimStart('.').ToLower();
        if (!Extensions.Contains(extension))
        {
            return [];
        }
        if (Path.GetFileName(path)!="app.xml")
        {
            return [];
        }
        string DirectoryName = Path.GetDirectoryName(path);

        Rom rom = new();
        string appXmlPath = Path.Combine(DirectoryName, "app.xml");
        string codedir = Path.GetDirectoryName(path);
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

            List<string> images = [];
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
        rom.Path = path;

        rom.IsFolderFormat = true;
        list.Add(rom);
        return list;
    }

  
}
