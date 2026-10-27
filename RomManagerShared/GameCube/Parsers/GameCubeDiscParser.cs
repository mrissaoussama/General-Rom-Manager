using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
namespace RomManagerShared.GameCube.Parsers;

public class GameCubeDiscParser : IRomParser<GameCubeConsole>, IContentSniffer
{
    // Plain images start with the disc header; ciso/rvz/wia are containers and checked when opened.
    public bool CanParse(string path, ReadOnlySpan<byte> header) =>
        !FileMagic.HasExtension(path, "iso", "gcm") || FileMagic.IsGameCubeDisc(header);

    public List<string> Extensions { get; set; } = ["iso", "gcm", "ciso", "rvz", "wia"];

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
