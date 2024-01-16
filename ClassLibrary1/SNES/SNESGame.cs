using RomManagerShared.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.SNES
{
    public interface ISNESRom { }
    public class SNESGame:Game, ISNESRom
    {
        public SNESGame() : base()
        {
        }
    }

}
