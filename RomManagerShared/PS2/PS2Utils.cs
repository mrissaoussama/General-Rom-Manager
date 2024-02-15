using RomManagerShared.Base;
using System.Text.RegularExpressions;
namespace RomManagerShared.PS2;

public static class PS2Utils
{
    public static HashSet<string> Extensions { get; set; }

    static PS2Utils()
    {
        Extensions = Enumerable.Range(0, 100)
                                 .Select(n => n.ToString("D2"))
                                 .ToHashSet();
        Extensions.Add("iso");
    }
    public static Region? GetRegionFile(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }
        if (Path.GetExtension(path) == string.Empty)
        {
            return null;
        }
        string fileExtension = Path.GetExtension(path).TrimStart('.');
        if (!Extensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        Dictionary<Region, string> regionPatterns = new()
        {
        { Region.USA, @"^(SCUS|SLUS)" },
        { Region.Europe, @"^(SCES|SLES|SCED)" },
        { Region.Japan, @"^(SCPS|SLPS|SLPM|SIPS)" }
    };

        foreach (var kvp in regionPatterns)
        {
            var filewithextension = Path.GetFileName(path);
            if (Regex.IsMatch(filewithextension, kvp.Value))
            {
                return kvp.Key;
            }
        }

        return Region.Unknown;
    }

    public static bool IsPS2TitleIDFile(string filename)
    {
        string fileName = Path.GetFileName(filename);
        var extensionIsSupported = Extensions.Any(ext => filename.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        return extensionIsSupported;
    }}