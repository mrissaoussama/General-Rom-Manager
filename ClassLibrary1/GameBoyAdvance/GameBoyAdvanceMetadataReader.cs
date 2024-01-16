using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibHac.Sf;
using RomManagerShared.GameBoy;
using RomManagerShared.Utils;

namespace RomManagerShared.GameBoyAdvance
{
    public class GameBoyAdvanceMetadataReader
    {
      
        private const int HEADER_LENGTH = 192; // 0xC0
        private const int TITLE_OFFSET = 160; // 0xA0
        private const int TITLE_LENGTH = 12;
        private const int GAME_CODE_OFFSET = 172; // 0xAC
        private const int GAME_CODE_LENGTH = 4;
        private const int MAKER_CODE_OFFSET = 176; // 0xB0
        private const int MAKER_CODE_LENGTH = 2;
        private const int UNIT_CODE_OFFSET = 179; // 0xB3
        private const int VERSION_CODE_OFFSET = 188; // 0xBC
        private const int HEADER_CHECKSUM_OFFSET = 189; // 0xBD

        public GameBoyAdvanceMetadata GetMetadata(string path)
        {
            byte[] gbaHeader;

            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                gbaHeader = new byte[HEADER_LENGTH];
                fileStream.Read(gbaHeader, 0, HEADER_LENGTH);
            }

            string title = BinUtils.AsciiToString(gbaHeader, TITLE_OFFSET, TITLE_LENGTH).Replace("\0", ""); ;
            string gameCode = BinUtils.AsciiToString(gbaHeader, GAME_CODE_OFFSET, GAME_CODE_LENGTH);
            string makerCode = BinUtils.AsciiToString(gbaHeader, MAKER_CODE_OFFSET, MAKER_CODE_LENGTH);
            string unitCode = BinUtils.ByteToHex(gbaHeader[UNIT_CODE_OFFSET]);
            string versionCode = BinUtils.ByteToHex(gbaHeader[VERSION_CODE_OFFSET]);
            string headerChecksum = BinUtils.ByteToHex(gbaHeader[HEADER_CHECKSUM_OFFSET]);

            GameBoyAdvanceMetadata metadata = new GameBoyAdvanceMetadata
            {
                Title = title,
                GameCode = gameCode,
                MakerCode = makerCode,
                UnitCode = unitCode,
                VersionCode = versionCode,
                HeaderChecksum = headerChecksum
            };

            return metadata;
        }
    }
}
