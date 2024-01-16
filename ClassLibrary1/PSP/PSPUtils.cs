using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using RomManagerShared.Utils;
using RomManagerShared.GameBoy;
using RomManagerShared.GameBoyAdvance;
using RomManagerShared.Base;
namespace RomManagerShared.PSP
{
    public static class PSPUtils
    {
        private static readonly string[] Extensions = ["iso"];


        public static bool IsPSPRom(string filePath, bool checkExtensionOnly = false)
        {
            return true;

        }
    }
    

}
