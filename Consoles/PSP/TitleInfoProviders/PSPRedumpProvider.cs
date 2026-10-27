using RomManagerShared.Base;
using RomManagerShared.Configuration;
using RomManagerShared.Interfaces;
using RomManagerShared.PSP;
using RomManagerShared.Utils.Dat;
using RomManagerShared.Utils;
using System.Buffers;
using System.Security.Cryptography;

namespace RomManagerShared.PSP.TitleInfoProviders;

public class PSPRedumpProvider : DiscDatTitleInfoProvider<PSPConsole>
{
    protected override bool MatchesDat(string f) => f.Contains("Sony - PlayStation Portable", StringComparison.OrdinalIgnoreCase);
}
