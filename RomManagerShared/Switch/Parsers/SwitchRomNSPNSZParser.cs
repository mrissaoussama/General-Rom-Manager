using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using System.Text;
using System.Xml;namespace RomManagerShared.Switch.Parsers;

public class SwitchRomNSPNSZParser : IRomParser<SwitchConsole>
{
    public List<string> Extensions { get; set; }
    List<Rom> RomList;
   private const int HeaderLength = 0xA00;    public SwitchRomNSPNSZParser()
    {
        Extensions = ["nsz", "nsp"];
        RomList = [];
    }
    public async Task<List<Rom>> ProcessFile(string switchRomPath)
    {
        List<Rom> list = [];
        List<string> ExtensionsData = [];
        using (FileStream fileStream = new(switchRomPath, FileMode.Open, FileAccess.Read))
        {
            byte[] headerBytes = new byte[HeaderLength];
            fileStream.Read(headerBytes, 0, HeaderLength);
            string headerAsString = Encoding.UTF8.GetString(headerBytes);
            string[] extensionsToFind = { ".tik", ".cert" };
            foreach (var extension in extensionsToFind)
            {
                int extensionIndex = headerAsString.IndexOf(extension, StringComparison.OrdinalIgnoreCase);
                while (extensionIndex != -1)
                {
                    string extensionData = headerAsString.Substring(Math.Max(0, extensionIndex - 32), 32);
                    extensionData = extensionData[..^16];
                    ExtensionsData.Add(extensionData.ToUpper());
                    extensionIndex = headerAsString.IndexOf(extension, extensionIndex + 1, StringComparison.OrdinalIgnoreCase);
                }
            }
            int contentMetaIndex = headerAsString.IndexOf("<ContentMeta>", StringComparison.OrdinalIgnoreCase);
            if (contentMetaIndex != -1)
            {
                int contentMetaEndIndex = headerAsString.IndexOf("</ContentMeta>", contentMetaIndex, StringComparison.OrdinalIgnoreCase);
                if (contentMetaEndIndex != -1)
                {
                    string contentMetaString = headerAsString.Substring(contentMetaIndex, contentMetaEndIndex - contentMetaIndex + 14);
                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(contentMetaString);
                    string? type = xmlDoc.SelectSingleNode("//Type")?.InnerText;
                    string? id = xmlDoc.SelectSingleNode("//Id")?.InnerText.Replace("0x", string.Empty).ToUpper(System.Globalization.CultureInfo.CurrentCulture);
                    string? version = xmlDoc.SelectSingleNode("//Version")?.InnerText;
                    string? requiredDownloadSystemVersion = xmlDoc.SelectSingleNode("//RequiredDownloadSystemVersion")?.InnerText;
                    string? requiredSystemVersion = xmlDoc.SelectSingleNode("//RequiredSystemVersion")?.InnerText;                    Rom? rom = list.FirstOrDefault(x => x.TitleID == id);
                    if (rom is null)
                    {
                        var romtype = SwitchUtils.GetRomMetadataClass(id);
                        rom = Activator.CreateInstance(romtype) as Rom;
                    }
                    rom.TitleID = id;
                    rom.Path = switchRomPath;
                    rom.Version = version;
                    rom.MinimumFirmware = requiredSystemVersion;
                    if (list.FirstOrDefault(x => x.TitleID == rom.TitleID) is null)

                        list.Add(rom);
                }
            }
        }
        foreach (var id in ExtensionsData)        {
            Rom? rom = list.FirstOrDefault(x => x.TitleID == id);
            if (rom is null)
            {
                var romtype = SwitchUtils.GetRomMetadataClass(id);
                rom = Activator.CreateInstance(romtype) as Rom;
            }
            rom.TitleID = id;
            rom.Path = switchRomPath;
            if (list.FirstOrDefault(x => x.TitleID == rom.TitleID) is null)

                list.Add(rom);
        }
        RomList.AddRange(list);
        return list;    }}
