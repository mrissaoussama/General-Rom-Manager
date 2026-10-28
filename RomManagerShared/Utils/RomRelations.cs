using System.Globalization;
using RomManagerShared.Base;

namespace RomManagerShared.Utils;

/// <summary>Links the library's updates and DLC to their base game.</summary>
public static class RomRelations
{
    /// <summary>
    /// The title ID of the game a title belongs to. Nintendo 3DS / Wii U IDs (<c>0004000E…</c>, <c>0005000C…</c>)
    /// keep their low word under the <c>00040000</c>/<c>00050000</c> category; Switch IDs (<c>01…</c>) drop the
    /// update/DLC bits (<c>…800</c>, <c>…1xxx</c>). Other consoles' updates reuse the game's ID (PSP, PS3, Vita, PS4).
    /// </summary>
    public static string? BaseTitleId(string? titleId)
    {
        if (string.IsNullOrWhiteSpace(titleId)) return null;
        var id = titleId.Trim().ToUpperInvariant();
        if (id.Length == 16 && ulong.TryParse(id, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
        {
            if (id.StartsWith("0004", StringComparison.Ordinal) || id.StartsWith("0005", StringComparison.Ordinal))
                return id[..4] + "0000" + id[8..];
            if (id.StartsWith("01", StringComparison.Ordinal))
                return (value & ~0x1FFFUL).ToString("X16", CultureInfo.InvariantCulture);
        }
        return id;
    }

    /// <summary>
    /// Fills <see cref="Game.Updates"/>, <see cref="Game.DLCs"/> and the updates' and DLC's
    /// <c>RelatedGame</c> from <paramref name="roms"/> (links from an earlier call are replaced). Updates and DLC
    /// without an icon take their game's: update and DLC packages rarely carry one.
    /// </summary>
    public static void Link(IEnumerable<Rom> roms)
    {
        var list = roms as IReadOnlyCollection<Rom> ?? roms.ToList();
        var games = new Dictionary<string, Game>(StringComparer.Ordinal);
        foreach (var game in list.OfType<Game>())
        {
            game.Updates = null;
            game.DLCs = null;
            if (BaseTitleId(game.TitleID) is { } id) games.TryAdd(id, game);
        }
        foreach (var rom in list)
        {
            Game? game = null;
            if (rom is not (Update or DLC) || BaseTitleId(rom.TitleID) is not { } id || !games.TryGetValue(id, out game))
                game = null;
            switch (rom)
            {
                case Update update:
                    update.RelatedGame = game;
                    if (game is not null) (game.Updates ??= []).Add(update);
                    break;
                case DLC dlc:
                    dlc.RelatedGame = game;
                    if (game is not null) (game.DLCs ??= []).Add(dlc);
                    break;
            }
            if (game is not null && string.IsNullOrWhiteSpace(rom.Icon))
                rom.Icon = game.Icon ?? game.Thumbnail;
        }
    }
}
