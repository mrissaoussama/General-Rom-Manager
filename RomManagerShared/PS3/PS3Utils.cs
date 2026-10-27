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
                    AppLog.Warn($"{vpkSfoFilePath} not found in the vpk archive.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AppLog.Warn(ex.Message);
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
        var sfo = ParamSfo.Read(memoryStream.ToArray());
        return sfo.Platform == SfoPlatform.PS3;
    }
    public static bool IsPS3SFO(byte[] memoryStream)
    {
        return ParamSfo.Read(memoryStream).Platform == SfoPlatform.PS3;
    }
}
