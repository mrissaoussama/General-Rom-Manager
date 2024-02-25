using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
namespace RomManagerShared.OriginalXbox.Parsers;

public class OriginalXboxRomParser : IRomParser<OriginalXboxConsole>
{
    public OriginalXboxRomParser()
    {
        Extensions = ["xbe"];
    }
    public List<string> Extensions { get; set; }
    //not sure of the compatibility as there are multiple cert versions
    public Task<List<Rom>> ProcessFile(string path)
    {
        OriginalXboxGame originalxboxrom = new();
        var metadata = XbeReader.ReadCertificate(File.ReadAllBytes(path));

        originalxboxrom.Version = metadata.Version.ToString();
        originalxboxrom.AddTitleName(metadata.TitleNameString.RemoveTrailingNullTerminators());
        originalxboxrom.TitleID = metadata.TitleIDHex;
        originalxboxrom.Size = FileUtils.GetFileSize(path);
        originalxboxrom.Path = path;
        originalxboxrom.IsFolderFormat = true;
        Console.WriteLine(originalxboxrom.ToString());
        List<Rom> list = [originalxboxrom];
        return Task.FromResult(list);
    }
}
