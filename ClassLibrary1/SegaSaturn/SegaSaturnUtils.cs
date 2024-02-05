using LibHac.Bcat;
using RomManagerShared.Utils;
using System.Text;

namespace RomManagerShared.SegaSaturn
{
    public static class SegaSaturnUtils
    {
        public static bool IsSegaSaturnRom(string filePath, bool checkExtensionOnly = false)
        {
            try
            {
                byte[] buffer = new byte[32];
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    int bytesRead = fileStream.Read(buffer, 0, buffer.Length);

                    string fileContent = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    return fileContent.Contains("SEGASATURN");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                FileUtils.Log("Error checking for sega saturn rom " + ex.Message);
                return false;
            }

        }    }}
