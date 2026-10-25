using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
namespace RomManagerShared.GameCube.Parsers;

public class GameCubeDiscParser : IRomParser<GameCubeConsole>
{
    public List<string> Extensions { get; set; } = ["iso", "gcm", "ciso"];

    public Task<List<Rom>> ProcessFile(string path)
    {
        try
        {
            var disc = NintendoDiscHeader.Read(path);
            if (disc is not { Kind: NintendoDiscKind.GameCube })
                return Task.FromResult<List<Rom>>([]);

            var game = new GameCubeGame
            {
                TitleID = disc.GameId,
                Version = disc.Version.ToString(),
                DiscNumber = disc.DiscNumber,
                Path = path,
                Size = FileUtils.GetFileSize(path),
            };
            game.AddTitleName(disc.Title);
            game.AddRegion(disc.Region);
            return Task.FromResult<List<Rom>>([game]);
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[GameCube] {path}: {ex.Message}");
            return Task.FromResult<List<Rom>>([]);
        }
    }
}
