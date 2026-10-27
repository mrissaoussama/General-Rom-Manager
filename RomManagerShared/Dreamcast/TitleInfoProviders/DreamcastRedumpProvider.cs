using RomManagerShared.Base;
using RomManagerShared.Configuration;
using RomManagerShared.Dreamcast;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils.Dat;
using RomManagerShared.Utils;
using System.Buffers;
using System.Security.Cryptography;

namespace RomManagerShared.Dreamcast.TitleInfoProviders;

public class DreamcastRedumpProvider : DiscDatTitleInfoProvider<DreamcastConsole>
{
    protected override bool MatchesDat(string f) => f.Contains("Sega - Dreamcast", StringComparison.OrdinalIgnoreCase);
}
