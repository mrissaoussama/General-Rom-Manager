﻿using RomManagerShared.Base;
using RomManagerShared.Nintendo64.Z64Utils;
using RomManagerShared.PS4;
using RomManagerShared.PSP;
using RomManagerShared.PSVita;
using RomManagerShared.PSVITA;
using RomManagerShared.SegaSaturn;
using RomManagerShared.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.PSVita.Parsers
{
    public class PSVitaVPKRomParser : IRomParser
    {
        public PSVitaVPKRomParser()
        {
            Extensions = ["vpk"];
        }
        public HashSet<string> Extensions { get; set; }
        //string[] gameCategories = { "AC", "GC", "GDC" };
        public Task<HashSet<Rom>> ProcessFile(string path)
        {
            HashSet<Rom> list = [];
            if (Path.GetExtension(path).Contains("vpk"))
            {
                try
                {
                    using (ZipArchive zipArchive = ZipFile.OpenRead(path))
                    {
                        ZipArchiveEntry? sfoEntry = zipArchive.GetEntry(PSVitaUtils.vpkSfoFilePath);

                        if (sfoEntry != null)
                        {
                            using (Stream sfoStream = sfoEntry.Open())
                            {
                                using (MemoryStream memoryStream = new MemoryStream())
                                {
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
                            }
                        }
                        else
                        {
                            FileUtils.Log($"{PSVitaUtils.vpkSfoFilePath} not found in the vpk archive.");
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

        private Region GetRegion(string region)
        {
            return region switch
            {
                "U" => Region.USA,
                "JT" => Region.Japan,
                "J" => Region.Japan,
                "JE" => Region.Europe,
                _ => Region.Unknown,
            } ;
        }
    }
}