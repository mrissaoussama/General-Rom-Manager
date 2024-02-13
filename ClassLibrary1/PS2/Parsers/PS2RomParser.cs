using DiscUtils.Iso9660;
using RomManagerShared.Base;
using RomManagerShared.Utils;
using System.Text.RegularExpressions;
namespace RomManagerShared.PS2.Parsers;

public class PS2RomParser : IRomParser
{
    public PS2RomParser()
    {
        Extensions = PS2Utils.Extensions;
    }
    public HashSet<string> Extensions { get; set; }
  
    public Task<HashSet<Rom>> ProcessFile(string path)
    {
        HashSet<Rom> list = [];

        PS2Game gameboyrom = new();
        if (!PS2Utils.IsPS2TitleIDFile(path))
            return Task.FromResult(list);
        string extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        if(!extension.Equals("iso"))
        {
            var region = PS2Utils.GetRegionFile(path);
            if(region is null)
                return Task.FromResult(list);
            PS2Game game = new()
            {
                TitleID = Path.GetFileName(path),
                IsFolderFormat = true
            };
            game.AddRegion((Region)region);
            game.Path = path;
            list.Add(game);
        }
        if (extension.Equals("iso"))
        {
            using var isoStream = new FileStream(path, FileMode.Open);
            var isoFile = new CDReader(isoStream, true);

            foreach (var file in isoFile.Root.GetFiles()
     .Select(file => file.Name.Split(';')[0]))
            {
                var region = PS2Utils.GetRegionFile(file);
                if (region is null)
                    continue;
                PS2Game game = new()
                {
                    TitleID = file,
                    IsFolderFormat = true,
                    Path = path
                };
                game.AddRegion((Region)region);
                list.Add(game);
            }

        }
        return Task.FromResult(list);

    }
}
