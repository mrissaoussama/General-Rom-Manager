using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.PSVITA;
using RomManagerShared.Utils;

namespace RomManagerShared.PSVita.Parsers;

public class PSVitaFolderRomParser : IRomParser<PSVitaConsole>
{
    public PSVitaFolderRomParser()
    {
        Extensions = ["sfo"];

    }
    public List<string> Extensions { get; set; }
    //string[] gameCategories = { "AC", "GC", "GDC" };

    public Task<List<Rom>> ProcessFile(string path)
    {
        List<Rom> list = [];
        if (Path.GetFileName(path).Equals("param.sfo", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                // Set ROM path to the directory containing sce_sys folder
                string romRootPath = Directory.GetParent(Path.GetDirectoryName(path)).FullName;

                using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read);
                using MemoryStream memoryStream = new();
                fileStream.CopyTo(memoryStream);

                if (PSVitaUtils.IsPSVitaSFO(memoryStream))
                {
                    Rom? vitarom = PSVitaSFOReader.ParseSFO(memoryStream.ToArray());
                    if (vitarom is not null)
                    {
                        vitarom.IsFolderFormat = true;
                        vitarom.Path = romRootPath; // Set to ROM root directory
                        list.Add(vitarom);
                    }
                }
            }
            catch (Exception ex)
            {
                FileUtils.Log(ex.Message);
            }
        }
        return Task.FromResult(list);
    }

}
