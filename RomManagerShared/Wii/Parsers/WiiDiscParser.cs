using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
namespace RomManagerShared.Wii.Parsers;

public class WiiDiscParser : IRomParser<WiiConsole>
{
    public List<string> Extensions { get; set; } = ["iso", "ciso", "rvz", "wia"];

    public Task<List<Rom>> ProcessFile(string path)
    {
        try
        {
            var disc = NintendoDiscHeader.Read(path);
            if (disc is not { Kind: NintendoDiscKind.Wii })
                return Task.FromResult<List<Rom>>([]);

            var game = new WiiGame
            {
                TitleID = disc.GameId,
                Version = disc.Version.ToString(),
                Path = path,
                Size = FileUtils.GetFileSize(path),
            };
            game.AddTitleName(disc.Title);
            game.AddRegion(disc.Region);
            return Task.FromResult<List<Rom>>([game]);
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[Wii] {path}: {ex.Message}");
            return Task.FromResult<List<Rom>>([]);
        }
    }
}
