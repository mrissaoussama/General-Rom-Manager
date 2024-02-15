using RomManagerShared.Utils;
using System.IO.Compression;

namespace RomManagerShared.PS3;

public static class PS3Utils
{
    public static string vpkSfoFilePath = "sce_sys/param.sfo";
    public static bool IsPS3Rom(string filePath, bool checkExtensionOnly = false)
    {
        if (Path.GetExtension(filePath).Contains("pkg"))
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

                    return IsPS3SFO(memoryStream);
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

            return IsPS3SFO(memoryStream);
        }
        return false;
    }
    public static bool IsPS3SFO(MemoryStream memoryStream)
    {
        Param_SFO.PARAM_SFO sfo = new(memoryStream);
        return /*sfo.TitleID.Contains("NP") || */sfo.PlaystationVersion == Param_SFO.PARAM_SFO.Playstation.ps3;
    }
    public static bool IsPS3SFO(byte[] memoryStream)
    {
        Param_SFO.PARAM_SFO sfo = new(memoryStream);
        return sfo.PlaystationVersion == Param_SFO.PARAM_SFO.Playstation.ps3;
    }}