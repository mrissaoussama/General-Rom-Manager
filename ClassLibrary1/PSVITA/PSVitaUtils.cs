using RomManagerShared.Utils;
using System.IO.Compression;

namespace RomManagerShared.PSVita;

public static class PSVitaUtils
{
    public static string vpkSfoFilePath = "sce_sys/param.sfo";
    public static bool IsPSVitaRom(string filePath, bool checkExtensionOnly = false)
    {
        if (Path.GetExtension(filePath).Contains("vpk"))
        {
            try
            {
                using ZipArchive zipArchive = ZipFile.OpenRead(filePath);
                ZipArchiveEntry? sfoEntry = zipArchive.GetEntry(vpkSfoFilePath);

                if (sfoEntry != null)
                {
                    using Stream sfoStream = sfoEntry.Open();
                    using MemoryStream memoryStream = new();
                    sfoStream.CopyTo(memoryStream);

                    return IsPSVitaSFO(memoryStream);
                }
                else
                {
                    FileUtils.Log($"{vpkSfoFilePath} not found in the vpk archive.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                FileUtils.Log(ex.Message);
                return false;
            }
        }
        else if (Path.GetExtension(filePath).Contains("sfo"))
        {
            using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
            using MemoryStream memoryStream = new();
            fileStream.CopyTo(memoryStream);

            return IsPSVitaSFO(memoryStream);
        }
        return false;
    }
    public static bool IsPSVitaSFO(MemoryStream memoryStream)
    {
        Param_SFO.PARAM_SFO sfo = new(memoryStream);
        return sfo.TitleID.Contains("PCS") || sfo.PlaystationVersion == Param_SFO.PARAM_SFO.Playstation.psvita;
    }
    public static bool IsPSVitaSFO(byte[] memoryStream)
    {
        Param_SFO.PARAM_SFO sfo = new(memoryStream);
        return sfo.PlaystationVersion == Param_SFO.PARAM_SFO.Playstation.psvita;
    }}