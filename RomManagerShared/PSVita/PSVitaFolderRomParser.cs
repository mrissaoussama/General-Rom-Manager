﻿using RomManagerShared.Base;
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
        if (Path.GetExtension(path).Contains("sfo"))
        {
            try
            {
                using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read);
                using MemoryStream memoryStream = new();
                fileStream.Seek(0, SeekOrigin.Begin);
                fileStream.CopyTo(memoryStream);
                var sfodata = memoryStream.ToArray();
                if (PSVitaUtils.IsPSVitaSFO(memoryStream))
                {
                    Rom? vitarom = PSVitaSFOReader.ParseSFO(sfodata);
                    if (vitarom is not null)
                    {
                        vitarom.IsFolderFormat = true;
                        vitarom.Path = path;
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
