using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.SNES
{
    public class SNESMetadata
    {
        public enum BankTypeEnum { Lo, Hi };
        public string Name;
        public byte Layout;
        public byte CartridgeType;
        public byte RomSize;
        public byte RamSize;
        public byte CountryCode;
        public byte LicenseCode;
        public byte VersionNumber;
        public ushort Checksum;
        public ushort ChecksumCompliment;
        public BankTypeEnum BankType;
    }
}
