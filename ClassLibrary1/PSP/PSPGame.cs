using RomManagerShared.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.PSP
{
    public interface IPSPRom { }
    public class PSPGame:Game, IPSPRom
    {
        public PSPGame() : base()
        {
        }

        public int DiskNumber { get; internal set; }
        public int DiskTotal { get; internal set; }
        public int ParentalLevel { get; internal set; }
    }

}
