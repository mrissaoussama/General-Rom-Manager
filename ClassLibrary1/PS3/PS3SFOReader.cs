using Param_SFO;
using RomManagerShared.Base;
using RomManagerShared.PS4;
using RomManagerShared.PS3;

namespace RomManagerShared.PSVITA;

public static class PS3SFOReader
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
        if (sfo.Category.ToUpper().Contains("HG") ||
            sfo.Category.ToUpper().Contains("2G") ||
            sfo.Category.ToUpper().Contains("2P") ||
            sfo.Category.ToUpper().Contains("1P") ||
            sfo.Category.ToUpper().Contains("MN") ||
            sfo.Category.ToUpper().Contains("PE"))
        {
            psvitarom = new PS3Game();
        }
        else if (sfo.Category.ToUpper().Contains("GD") || sfo.Category.ToUpper().Contains("2D"))
        {
            psvitarom = new PS3DLC();
        }
        else 
        {
            psvitarom = new PS3Update();
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
