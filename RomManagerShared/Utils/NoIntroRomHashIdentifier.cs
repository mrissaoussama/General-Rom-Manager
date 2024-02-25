using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.Utils
{
    public class NoIntroRomHashIdentifier
    {
        public NoIntroRomHashIdentifier() { }
        public List<RomHash> IdentifyRomByHash(GamingConsole gamingConsole, string path)
        {
            switch (gamingConsole)
            {
                case SNESConsole:
                    return IdentifySNES(path);
                    break;
                default: return [];
            }
        }

        public List<RomHash> IdentifySNES(string path)
        {
            throw new NotImplementedException();
        }
    }
}
