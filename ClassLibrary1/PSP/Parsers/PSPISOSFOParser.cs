using DiscUtils.Iso9660;using Param_SFO;using RomManagerShared.Base;
using RomManagerShared.PSP;using RomManagerShared.Utils.PKGUtils;using System.Text;
namespace RomManagerShared.PSP.Parsers;

public class PSPISOSFOParser : IRomParser
{
    public PSPISOSFOParser()
    {
        Extensions = ["iso"];
    }    public HashSet<string> Extensions { get; set; }    public Task<HashSet<Rom>> ProcessFile(string path)
    {
        PSPGame pspRom = new();        try
        {
            using FileStream isoStream = File.Open(path, FileMode.Open, FileAccess.Read);
            CDReader cd = new(isoStream, true);
            Guid newGuid = Guid.NewGuid();
            var sfopath = Path.Combine(RomManagerConfiguration.GetPSPCachePath(), newGuid.ToString() + ".sfo");
            //cd.CopyFile(@"PSP_GAME\PARAM.SFO", sfopath);
          Stream stream= cd.OpenFile(@"PSP_GAME\PARAM.SFO", FileMode.Open);
            using MemoryStream memoryStream = new MemoryStream();
             stream.CopyTo(memoryStream);
                byte[] byteArray = memoryStream.ToArray();
                var rom = ParseSFO(byteArray);
            List<byte> titleNameBytes = [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }        HashSet<Rom> romList = [pspRom];
        return Task.FromResult(romList);
    }
    public static Rom? ParseSFO(MemoryStream stream)
    {
        Param_SFO.PARAM_SFO sfo = new(stream);


        //   Rom? psvita = GetData(sfo);
        // return psvita;
        return null;

    }
    public static Rom? ParseSFO(byte[] stream)
    {
        Param_SFO.PARAM_SFO sfo = new(stream);

        //   Rom? psvita = GetData(sfo);
        // return psvita;
        return null;
    }


    //private static Rom? GetData(PARAM_SFO sfo)
    //{

    //    Rom? psvitarom = null;
    //    if (sfo.Category.ToUpper().Contains("UG"))
    //    {
    //        psvitarom = new PSVitaGame();
    //    }
    //    else if (sfo.Category.ToUpper().Contains("GC") || sfo.Category.ToUpper().Contains("AC"))
    //    {
    //        psvitarom = new PSVitaDLC();
    //    }
    //    else if (sfo.Category.ToUpper().Contains("GP"))
    //    {
    //        psvitarom = new PSVitaUpdate();
    //    }
    //    if (psvitarom == null) return null;
    //    psvitarom.TitleID = sfo.TitleID;
    //    psvitarom.ProductCode = sfo.ContentID;
    //    psvitarom.AddTitleName(sfo.Title);
    //    psvitarom.Version = sfo.APP_VER;
    //    Param_SFO.PARAM_SFO.Table t = sfo.Tables.ToList().Where(x => x.Name == "SYSTEM_VER").FirstOrDefault();
    //    if (t.Name is not null)
    //    {
    //        long value = Convert.ToInt64(t.Value);
    //        psvitarom.MinimumFirmware = PS4Utils.SystemFirmwareLongToString(value);
    //    }
    //    return psvitarom;
    //}
    private static void SetRegion(PSPGame pspRom)
    {
        switch (pspRom.TitleID![2])
        {
            case 'U':
                pspRom.AddRegion(Region.USA);
                break;
            case 'E':
                pspRom.AddRegion(Region.Europe);
                break;
            case 'J':
                pspRom.AddRegion(Region.Japan);
                break;
            case 'A':
                pspRom.AddRegion(Region.Asia);
                break;
            case 'H':
                pspRom.AddRegion(Region.HongKong);
                break;
            case 'K':
                pspRom.AddRegion(Region.Korea);
                break;
            default:
                pspRom.AddRegion(Region.Unknown);
                break;
        }    }
}
