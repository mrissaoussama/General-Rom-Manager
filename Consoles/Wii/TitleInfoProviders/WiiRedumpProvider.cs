using RomManagerShared.Base;
using RomManagerShared.Configuration;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils.Dat;
using RomManagerShared.Utils;
using RomManagerShared.Wii;
using System.Buffers;
using System.Security.Cryptography;

namespace RomManagerShared.Wii.TitleInfoProviders;

public class WiiRedumpProvider : DiscDatTitleInfoProvider<WiiConsole>
{
    protected override bool MatchesDat(string f) =>
        f.Contains("Nintendo - Wii", StringComparison.OrdinalIgnoreCase) && !f.Contains("Wii U", StringComparison.OrdinalIgnoreCase);
}
