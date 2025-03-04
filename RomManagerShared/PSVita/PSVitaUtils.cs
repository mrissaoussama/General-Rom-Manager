using RomManagerShared.PSVITA;
using RomManagerShared.Utils;
using System.IO;
using System.IO.Compression;
using System.Text;

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
    /// <summary>
    /// Checks if a directory contains nested Vita content that doesn't belong
    /// </summary>
    public static bool HasUnrelatedNestedContent(string basePath, string parentTitleId)
    {
        return Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories)
            .Any(d => IsPSVitaRomFolder(d) &&
                 GetBaseTitleIdFromFolder(d) != parentTitleId);
    }

    /// <summary>
    /// Extracts base Title ID from folder structure
    /// </summary>
    public static string GetBaseTitleIdFromFolder(string folderPath)
    {
        var sfoPath = Path.Combine(folderPath, "sce_sys", "param.sfo");
        if (!File.Exists(sfoPath)) return null;

        using var fs = new FileStream(sfoPath, FileMode.Open);
        using MemoryStream memoryStream = new();
        fs.CopyTo(memoryStream);

        var rom = PSVitaSFOReader.ParseSFO(memoryStream);
        return rom?.TitleID?.Length >= 9 ? rom.TitleID[..9] : rom?.TitleID;
    }
    public static bool IsPSVitaRomFolder(string folderPath)
    {
        try
        {
            // Check for required Vita files
            var sfoPath = Path.Combine(folderPath, "sce_sys", "param.sfo");
            if (!File.Exists(sfoPath)) return false;

            // Verify SFO content is Vita-specific
            using var fs = new FileStream(sfoPath, FileMode.Open);
            using MemoryStream memoryStream = new();
            fs.CopyTo(memoryStream);
            return IsPSVitaSFO(memoryStream);
        }
        catch
        {
            return false;
        }
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
    }
    /// <summary>
    /// Reads a rif file and parses the PSVitaLicense data.
    /// From offset 0x17 to 0x1F (9 bytes) is the TitleID.
    /// From offset 0x24 to 0x33 (16 bytes) is the LicenseId.
    /// </summary>
    /// <param name="rifPath">The path to the rif file.</param>
    /// <returns>A PSVitaLicense instance with TitleID and LicenseId set.</returns>
    public static PSVitaLicense GetVitaLicenseData(string rifPath)
    {
        // Read all bytes from the file.
        byte[] data = File.ReadAllBytes(rifPath);

        // Ensure the file is large enough.
        // has to be 512 bytes in size


        if (data.Length <512)
        {
            throw new InvalidDataException("RIF file is too short to contain license data.");
        }

        // Extract title id from offset 0x17 to 0x1F (inclusive = 9 bytes).
        string titleId = Encoding.ASCII.GetString(data, 0x17, 0x1F - 0x17 + 1).TrimEnd('\0');

        // Extract license id from offset 0x24 to 0x33 (inclusive = 16 bytes).
        string licenseId = Encoding.ASCII.GetString(data, 0x24, 0x33 - 0x24 + 1).TrimEnd('\0');

        // Create the license object.
        var license = new PSVitaLicense(rifPath)
        {
            TitleID = titleId,
            LicenseId = licenseId,
           
        };

        return license;
    }
}