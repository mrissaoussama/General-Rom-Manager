using DiscUtils;
using DiscUtils.Iso9660;
using DiscUtils.Udf;
using RomManagerShared.Utils;
using RomManagerShared.Base;
using RomManagerShared.Interfaces;
namespace RomManagerShared.PS2.Parsers;

public class PS2RomParser : IRomParser<PS2Console>
{
    public PS2RomParser()
    {
        Extensions = PS2Utils.Extensions;
    }
    public List<string> Extensions { get; set; }
  
    public Task<List<Rom>> ProcessFile(string path)
    {
        List<Rom> list = [];

        PS2Game gameboyrom = new();
        if (!PS2Utils.IsPS2TitleIDFile(path))
            return Task.FromResult(list);
        string extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        bool isImage = extension is "iso" or "cso";
        if (!isImage)
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
        if (isImage)
        {
            using var isoStream = FileUtils.OpenDiscImage(path);
            using DiscFileSystem? disc = CDReader.Detect(isoStream) ? new CDReader(isoStream, true)
                : UdfReader.Detect(isoStream) ? new UdfReader(isoStream)
                : null;
            if (disc is null)
                return Task.FromResult(list);

            // SYSTEM.CNF names the boot ELF, which is the title ID. Fall back to scanning the root.
            var bootFile = ReadBootFile(disc);
            if (bootFile is not null && PS2Utils.GetRegionFile(bootFile) is null)
                bootFile = null; // e.g. cheat discs booting a custom ELF
            var candidates = bootFile is not null
                ? [bootFile]
                : disc.Root.GetFiles().Select(file => file.Name.Split(';')[0]);
            foreach (var file in candidates)
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
                if (bootFile is not null) break;
            }
        }
        return Task.FromResult(list);

    }
    // BOOT2 = cdrom0:\SLUS_200.90;1
    private static string? ReadBootFile(DiscFileSystem disc)
    {
        if (!disc.FileExists("SYSTEM.CNF"))
            return null;
        using var reader = new StreamReader(disc.OpenFile("SYSTEM.CNF", FileMode.Open, FileAccess.Read));
        while (reader.ReadLine() is { } line)
        {
            if (!line.TrimStart().StartsWith("BOOT2", StringComparison.OrdinalIgnoreCase))
                continue;
            var value = line[(line.IndexOf('=') + 1)..].Trim();
            var name = value[(value.LastIndexOfAny([':', '\\', '/']) + 1)..];
            return name.Split(';')[0];
        }
        return null;
    }
}
