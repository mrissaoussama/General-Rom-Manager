using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using System.Text;

namespace RomManagerShared.WiiU.Parsers;

public class WiiUWudWuxParser : IRomParser<WiiUConsole>
{
    public List<string> Extensions { get; set; }

    public WiiUWudWuxParser()
    {
        Extensions = ["wud", "wux"];
    }

    public async Task<List<Rom>> ProcessFile(string path)
    {
        List<Rom> list = [];
        string extension = Path.GetExtension(path)?.TrimStart('.').ToLower();

        if (!Extensions.Contains(extension))
        {
            return list;
        }

        string productCode;
        if (extension == "wud")
        {
            productCode = await GetProductCodeFromOffset(path, 0x0);
        }
        else 
        {
            productCode = await GetProductCodeFromOffset(path, 0x2F0000);
        }

        if (!productCode.StartsWith("WUP"))
        {
            return list;
        }

        WiiUGame wiiUGame = new()
        { Path = path,
            ProductCode = productCode
        };
        list.Add(wiiUGame);
        return list;
    }

    private static async Task<string> GetProductCodeFromOffset(string filePath, long offset)
    {
        using FileStream fileStream = File.OpenRead(filePath);
        fileStream.Seek(offset, SeekOrigin.Begin);
        byte[] buffer = new byte[22];
        await fileStream.ReadAsync(buffer);
        return Encoding.ASCII.GetString(buffer);
    }
}
