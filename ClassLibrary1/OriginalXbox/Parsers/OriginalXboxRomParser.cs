using RomManagerShared.Base;
using RomManagerShared.Utils;
namespace RomManagerShared.OriginalXbox.Parsers;

public class OriginalXboxRomParser : IRomParser
{
    public OriginalXboxRomParser()
    {
        Extensions = ["xbe"];
    }
    public HashSet<string> Extensions { get; set; }
    //not sure of the compatibility as there are multiple cert versions
    public Task<HashSet<Rom>> ProcessFile(string path)
    {
        OriginalXboxGame originalxboxrom = new();
        var metadata = XbeReader.ReadCertificate(File.ReadAllBytes(path));

        originalxboxrom.Version = metadata.Version.ToString();
        originalxboxrom.AddTitleName(metadata.TitleNameString.RemoveTrailingNullTerminators());
        originalxboxrom.TitleID = metadata.TitleIDHex;
        originalxboxrom.Size = FileUtils.GetFileSize(path);
        originalxboxrom.Path = path;
        Console.WriteLine(originalxboxrom.ToString());
        HashSet<Rom> list = [originalxboxrom];
        return Task.FromResult(list);
    }
}
