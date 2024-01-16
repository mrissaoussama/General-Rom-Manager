using RomManagerShared.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.DS
{
    public interface IDSRom { }
    public class DSGame:Game, IDSRom
    {
        public DSGame() : base()
        {
        }
    }

}
