using LibHac.Bcat;

namespace RomManagerShared.SNES
{
    public static class SNESUtils
    {
        private static readonly string[] Extensions = ["sfc"];        public static bool IsSNESRom(string filePath, bool checkExtensionOnly = false)
        {
            string fileExtension = System.IO.Path.GetExtension(filePath).TrimStart('.');
           return Extensions.Contains(fileExtension);
                   }    }}
