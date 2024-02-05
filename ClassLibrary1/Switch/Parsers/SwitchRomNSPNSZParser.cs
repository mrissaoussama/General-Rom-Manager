using RomManagerShared.Base;
using System.Text;
using System.Xml;namespace RomManagerShared.Switch.Parsers
{
    public class SwitchRomNSPNSZParser : IRomParser
    {
        public HashSet<string> Extensions { get; set; }
        HashSet<Rom> RomList;
        string switchRomPath; private const int HeaderLength = 0xA00;        public SwitchRomNSPNSZParser()
        {
            Extensions = ["nsz", "nsp"];
            RomList = []; switchRomPath = string.Empty;
        }
        public async Task<HashSet<Rom>> ProcessFile(string switchRomPath)
        {
            HashSet<Rom> list = [];
            HashSet<string> extensionsData = new HashSet<string>();
            using (FileStream fileStream = new FileStream(switchRomPath, FileMode.Open, FileAccess.Read))
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
                        extensionData = extensionData.Substring(0, extensionData.Length - 16);
                        extensionsData.Add(extensionData.ToUpper());
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
                        string? requiredSystemVersion = xmlDoc.SelectSingleNode("//RequiredSystemVersion")?.InnerText;                        Rom? rom = list.FirstOrDefault(x => x.TitleID == id);
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
            foreach (var id in extensionsData)            {
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
            RomList.UnionWith(list);
            return list;        }    }
}
