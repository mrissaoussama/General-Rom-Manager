using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.PSVITA;
using RomManagerShared.Utils;
using System.IO.Compression;

namespace RomManagerShared.PSVita.Parsers;

public class PSVitaVPKRomParser : IRomParser<PSVitaConsole>
{
    public PSVitaVPKRomParser()
    {
        Extensions = ["vpk"];
    }
    public List<string> Extensions { get; set; }
    //string[] gameCategories = { "AC", "GC", "GDC" };
    public Task<List<Rom>> ProcessFile(string path)
    {
        List<Rom> list = [];
        if (Path.GetExtension(path).Contains("vpk"))
        {
            try
            {
                using ZipArchive zipArchive = ZipFile.OpenRead(path);
                ZipArchiveEntry? sfoEntry = zipArchive.GetEntry(PSVitaUtils.vpkSfoFilePath);

                if (sfoEntry != null)
                {
                    using Stream sfoStream = sfoEntry.Open();
                    using MemoryStream memoryStream = new();
                    sfoStream.CopyTo(memoryStream);
                    var sfodata = memoryStream.ToArray();
                    if (PSVitaUtils.IsPSVitaSFO(sfodata))
                    {
                        Rom? vitarom = PSVitaSFOReader.ParseSFO(memoryStream);
                        if (vitarom is not null)
                        {
                            vitarom.Path = path;
                            list.Add(vitarom);
                        }
                    }
                }
                else
                {
                    FileUtils.Log($"{PSVitaUtils.vpkSfoFilePath} not found in the vpk archive.");
                }
            }
            catch (Exception ex)
            {
                FileUtils.Log(ex.Message);

            }
        }
        return Task.FromResult(list);
    }

    private static Region GetRegion(string region)
    {
        return region switch
        {
            "U" => Region.USA,
            "JT" => Region.Japan,
            "J" => Region.Japan,
            "JE" => Region.Europe,
            _ => Region.Unknown,
        };
    }
}
