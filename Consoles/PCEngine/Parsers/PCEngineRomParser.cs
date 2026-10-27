using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;

namespace RomManagerShared.PCEngine.Parsers;

/// <summary>HuCards have no header; titles come from the file name or DATs (PCEngineDatProvider).</summary>
public class PCEngineRomParser : IRomParser<PCEngineConsole>
{
    public List<string> Extensions { get; set; } = ["pce"];

    public Task<List<Rom>> ProcessFile(string path)
    {
        try
        {
            var length = new FileInfo(path).Length;
            var game = new PCEngineGame
            {
                Path = path,
                Size = length,
                HeaderFormat = PCEngineGame.HasCopierHeader(length) ? "Copier header" : "None",
            };
            game.AddTitleName(Path.GetFileNameWithoutExtension(path));
            return Task.FromResult<List<Rom>>([game]);
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[PCEngine] {path}: {ex.Message}");
            return Task.FromResult<List<Rom>>([]);
        }
    }
}
