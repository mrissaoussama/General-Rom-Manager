using Param_SFO;
using RomManagerShared.Base;
using RomManagerShared.PS4;
using RomManagerShared.PSVita;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.PSVITA
{
    public static class PSVitaSFOReader
    {
        public static Rom? ParseSFO(MemoryStream stream)
        {
                Param_SFO.PARAM_SFO sfo = new(stream);

            Rom? psvita = GetData(sfo);
            return psvita;

        }
        public static Rom? ParseSFO(byte[] stream)
        {
            Param_SFO.PARAM_SFO sfo = new(stream);

            Rom? psvita = GetData(sfo);
            return psvita;

        }


        private static Rom? GetData(PARAM_SFO sfo)
        {
        
                Rom? psvitarom = null;
                if (sfo.Category.ToUpper().Contains("GD"))
                {
                    psvitarom = new PSVitaGame();
                }
                else if (sfo.Category.ToUpper().Contains("GC") || sfo.Category.ToUpper().Contains("AC"))
                {
                    psvitarom = new PSVitaDLC();
                }
                else if (sfo.Category.ToUpper().Contains("GP"))
                {
                    psvitarom = new PSVitaUpdate();
                }
                if (psvitarom == null) return null;
                psvitarom.TitleID = sfo.TitleID;
                psvitarom.ProductCode = sfo.ContentID;
                psvitarom.AddTitleName(sfo.Title);
                psvitarom.Version = sfo.APP_VER;
                Param_SFO.PARAM_SFO.Table t = sfo.Tables.ToList().Where(x => x.Name == "SYSTEM_VER").FirstOrDefault();
                if (t.Name is not null)
                {
                    long value = Convert.ToInt64(t.Value);
                    psvitarom.MinimumFirmware = PS4Utils.SystemFirmwareLongToString(value);
                }
                return psvitarom;
            }
        
    }
}
