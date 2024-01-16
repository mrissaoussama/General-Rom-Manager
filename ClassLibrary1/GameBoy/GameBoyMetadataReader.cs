using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibHac.Sf;
using RomManagerShared.Utils;
using static LibHac.FsSystem.BucketTree;

namespace RomManagerShared.GameBoy
{
    public class GameBoyMetadataReader
    {
        private const int HEADER_LENGTH = 335; // 0x014F
        private const int TITLE_OFFSET = 308; // 0x134
        private const int TITLE_LENGTH = 16;
        private const int GAME_CODE_OFFSET = 319; // 0x13F
        private const int GAME_CODE_LENGTH = 4;
        private const int CGB_FLAG_OFFSET = 323; // 0x143
        private const int CGB_FLAG_BIT = 7;
        private const int NEW_LICENSEE_CODE_OFFSET = 324; // 0x144
        private const int NEW_LICENSEE_CODE_LENGTH = 2;
        private const int SGB_FLAG_OFFSET = 326; // 0x146
        private const int CARTRIDGE_TYPE_OFFSET = 327; // 0x147
        private const int ROM_SIZE_OFFSET = 328; // 0x148
        private const int RAM_SIZE_OFFSET = 329; // 0x149
        private const int DESTINATION_CODE_OFFSET = 330; // 0x14A
        private const int OLD_LICENSEE_CODE_OFFSET = 331; // 0x14B
        private const int MASK_ROM_VERSION_NUMBER_OFFSET = 332; // 0x14C
        private const int HEADER_CHECKSUM_OFFSET = 333; // 0x14A
        private const int GLOBAL_CHECKSUM_OFFSET = 334; // 0x14E
        public GameBoyMetadata GetMetadata(string path)
        {
            byte[] gbaHeader;

            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                gbaHeader = new byte[HEADER_LENGTH];
                fileStream.Read(gbaHeader, 0, HEADER_LENGTH);
            }
            bool isCgb = (gbaHeader[CGB_FLAG_OFFSET] & (1 << CGB_FLAG_BIT)) != 0;

            string title = BinUtils.AsciiToString(gbaHeader, TITLE_OFFSET, TITLE_LENGTH);
            string gameCode = BinUtils.AsciiToString(gbaHeader, GAME_CODE_OFFSET, GAME_CODE_LENGTH);
            string cgbFlag = isCgb ? "CGB" : "Non-CGB"; // Customize this as needed
            string cgbFlagcode = BinUtils.ByteToHex(gbaHeader[CGB_FLAG_OFFSET]);

            string newLicenseeCode = BinUtils.AsciiToString(gbaHeader, NEW_LICENSEE_CODE_OFFSET, NEW_LICENSEE_CODE_LENGTH);
            string sgbFlag = BinUtils.ByteToHex(gbaHeader[SGB_FLAG_OFFSET]);
            string cartridgeType = BinUtils.ByteToHex(gbaHeader[CARTRIDGE_TYPE_OFFSET]);
            string romSize = BinUtils.ByteToHex(gbaHeader[ROM_SIZE_OFFSET]);
            string ramSize = BinUtils.ByteToHex(gbaHeader[RAM_SIZE_OFFSET]);
            string destinationCode = BinUtils.ByteToHex(gbaHeader[DESTINATION_CODE_OFFSET]);
            string oldLicenseeCode = BinUtils.ByteToHex(gbaHeader[OLD_LICENSEE_CODE_OFFSET]);
            string maskRomVersion = BinUtils.ByteToHex(gbaHeader[MASK_ROM_VERSION_NUMBER_OFFSET]);
            string headerChecksum = BinUtils.ByteToHex(gbaHeader[HEADER_CHECKSUM_OFFSET]);
            //might be wrong, documentation says it starts from 014E-014F
            byte[] globalChecksum = new byte[2];
            Array.Copy(gbaHeader, gbaHeader.Length - 2, globalChecksum, 0, 2);

            GameBoyMetadata metadata = new GameBoyMetadata
            {
                Title = title,
                GameCode = gameCode,
                CgbFlag = cgbFlag,
                CgbFlagcode= cgbFlagcode,
                NewLicenseeCode = newLicenseeCode,
                SgbFlag = sgbFlag,
                CartridgeType = cartridgeType,
                RomSize = romSize,
                RamSize = ramSize,
                DestinationCode = destinationCode,
                OldLicenseeCode = oldLicenseeCode,
                MaskRomVersionNumber = maskRomVersion,
                HeaderChecksum = headerChecksum,
                StoredGlobalChecksum= globalChecksum
            };

            return metadata;
        }
        public byte CalculateHeaderChecksum(byte[] romData)
        {
            byte checksum = 0;

            // Iterate through the header bytes 0134-014C
            for (int i = TITLE_OFFSET; i <= MASK_ROM_VERSION_NUMBER_OFFSET; i++)
            {
                checksum = (byte)(checksum - romData[i] - 1);
            }

            return checksum;
        }
        public ushort CalculateGlobalChecksum(byte[] romData)
        {
            ushort checksum = 0;

            // Iterate through all bytes in the ROM data (excluding the checksum bytes)
            for (int i = 0; i < romData.Length; i++)
            {
                if (i != GLOBAL_CHECKSUM_OFFSET && i != GLOBAL_CHECKSUM_OFFSET + 1)
                {
                    checksum += romData[i];
                }
            }

            return checksum;
        }
    }
}
