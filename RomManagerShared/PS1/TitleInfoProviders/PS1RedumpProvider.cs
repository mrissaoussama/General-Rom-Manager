using RomManagerShared.Base;
using RomManagerShared.Configuration;
using RomManagerShared.Interfaces;
using RomManagerShared.PS1;
using RomManagerShared.Utils.Dat;
using RomManagerShared.Utils;
using System.Buffers;
using System.Security.Cryptography;

namespace RomManagerShared.PS1.TitleInfoProviders;

public class PS1RedumpProvider : DiscDatTitleInfoProvider<PS1Console>
{
    // "Sony - PlayStation - Datfile", not PlayStation 2/3/Portable/Vita.
    protected override bool MatchesDat(string f) =>
        System.Text.RegularExpressions.Regex.IsMatch(f, @"Sony - PlayStation( -|\s*\(|\.)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}
