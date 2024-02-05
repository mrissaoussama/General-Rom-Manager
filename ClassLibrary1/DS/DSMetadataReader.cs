using RomManagerShared.Utils;
namespace RomManagerShared.DS
{
    public class DSMetadataReader
    {
        private const int HEADER_LENGTH = 100;
        private const int TITLE_OFFSET = 0;
        private const int TITLE_LENGTH = 12;
        private const int GAME_CODE_OFFSET = 12;
        private const int GAME_CODE_LENGTH = 4;
        private const int MAKER_CODE_OFFSET = 16;
        private const int MAKER_CODE_LENGTH = 2;
        private const int REGION_CODE_OFFSET = 0x1D;
        private const int ROM_VERSION_OFFSET = 0x1E;
        private const int CART_HEADER_OFFSET = 0x68;        private const int UNIT_CODE_OFFSET = 18;
        private const int ICON_BITMAP_OFFSET = 0x20;
        private const int ICON_BITMAP_SIZE = 0x200;
        private const int ICON_PALETTE_OFFSET = 0x220;
        private const int ICON_PALETTE_SIZE = 0x20;
        //i'm still not sure about the header and icon extraction, will do this later.
        public DSMetadata GetMetadata(string path)
        {
            byte[] ndsHeader;
            DSMetadata metadata = new();
            using (FileStream fileStream = new(path, FileMode.Open, FileAccess.Read))
            {
                ndsHeader = new byte[HEADER_LENGTH];
                fileStream.Read(ndsHeader, 0, HEADER_LENGTH);
                string title = BinUtils.AsciiToString(ndsHeader, TITLE_OFFSET, TITLE_LENGTH);
                string gameCode = BinUtils.AsciiToString(ndsHeader, GAME_CODE_OFFSET, GAME_CODE_LENGTH);
                string makerCode = BinUtils.AsciiToString(ndsHeader, MAKER_CODE_OFFSET, MAKER_CODE_LENGTH);
                string unitCode = BinUtils.ByteToHex(ndsHeader[UNIT_CODE_OFFSET]);
                string regionCode = BinUtils.ByteToHex(ndsHeader[REGION_CODE_OFFSET]);
                string romVersion = BinUtils.ByteToHex(ndsHeader[ROM_VERSION_OFFSET]);
                //byte[] cartridgeHeader = new byte[0x240];
                //fileStream.Seek(CART_HEADER_OFFSET, SeekOrigin.Begin);
                //fileStream.Read(cartridgeHeader, 0, 0x240);
                //    byte[] iconBitmap = new byte[ICON_BITMAP_SIZE];
                //    byte[] iconPalette = new byte[ICON_PALETTE_SIZE];
                //    Array.Copy(cartridgeHeader, ICON_BITMAP_OFFSET, iconBitmap, 0, ICON_BITMAP_SIZE);
                //    Array.Copy(cartridgeHeader, ICON_PALETTE_OFFSET, iconPalette, 0, ICON_PALETTE_SIZE);
                //// Read Titles
                //string[] titles = new string[8];
                //for (int i = 0; i < titles.Length; i++)
                //{
                //    byte[] titleBytes = new byte[0x100];
                //    fileStream.Seek(CART_HEADER_OFFSET + 0x240 + i * 0x100, SeekOrigin.Begin);
                //    fileStream.Read(titleBytes, 0, 0x100);
                //    titles[i] = Encoding.Unicode.GetString(titleBytes).TrimEnd('\0');
                //}
                //    Bitmap bmp = CreateBitmapFromIcon(iconBitmap, iconPalette);
                //    bmp.Save(""); saves a mostly black image, i don't know much about bitmaps
                metadata = new DSMetadata
                {
                    Title = title,
                    GameCode = gameCode,
                    MakerCode = makerCode,
                    UnitCode = unitCode,
                    RegionCode = regionCode,
                    RomVersion = romVersion,
                };
            }
            return metadata;
        }
        //private Bitmap CreateBitmapFromIcon(byte[] iconBitmap, byte[] iconPalette)
        //{
        //    Bitmap bmp = new Bitmap(32, 32);
        //    for (int y = 0; y < 32; y++)
        //    {
        //        for (int x = 0; x < 32; x++)
        //        {
        //            int pixelIndex = (y / 8) * 8 + x / 8;
        //            int colorIndex = (iconBitmap[pixelIndex] >> (4 * (7 - x % 8))) & 0x0F;
        //            // Assuming iconPalette is a ushort array
        //            int paletteEntryOffset = colorIndex * 2;
        //            ushort colorValue = (ushort)((iconPalette[paletteEntryOffset + 1] << 8) | iconPalette[paletteEntryOffset]);
        //            // Extract color components
        //            int red = (colorValue & 0x1F) << 3;
        //            int green = ((colorValue >> 5) & 0x3F) << 2;
        //            int blue = ((colorValue >> 11) & 0x1F) << 3;
        //            // Set the color to the Bitmap
        //            bmp.SetPixel(x, y, Color.FromArgb(red, green, blue));
        //        }
        //    }
        //    return bmp;
        //}
    }
}
