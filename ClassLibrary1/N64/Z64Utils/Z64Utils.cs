using System.Text;
using RomManagerShared.Base;
namespace RomManagerShared.Nintendo64.Z64Utils
{

   public class Z64Utils
    {
        public static Nintendo64Game ParseRom(string filePath)
        {
   Nintendo64RomMetadata n64Rom = new Nintendo64RomMetadata(filePath);
            Nintendo64Game game = new()
            {
                TitleID = n64Rom.CartID
           ,Path = filePath
            };  
            
            game.Version = n64Rom.Version.ToString();
                game.Developer = n64Rom.Developer;
            game.AddRegion(GetRegion(n64Rom.CountryCode));
        
                game.AddTitleName(n64Rom.Name);
            return game;
        }

        private static Region GetRegion(byte countryCode)
        {
            switch (countryCode)
            {
                case 0x41: return Region.Asia;
                case 0x42: return Region.Brazil;
                case 0x43: return Region.China;
                case 0x44: return Region.Germany;
                case 0x45: return Region.USA;
                case 0x46: return Region.France;
                //case 0x47: return "Gateway 64 (NTSC)";
                case 0x48: return Region.Netherlands;
                case 0x49: return Region.Italy;
                case 0x4A: return Region.Japan;
                case 0x4B: return Region.Korea;
                //  case 0x4C: return "Gateway 64 (PAL)";
                case 0x4E: return Region.Canada;
                case 0x50: return Region.Europe;
                case 0x53: return Region.Spain;
                case 0x55: return Region.Australia;
                case 0x57: return Region.Scandinavia;
                case 0x58: return Region.Europe;
                case 0x59: return Region.Europe;
                default: return Region.Unknown;
            }
        }
    }



  
    public enum Nintendo64CountryCode
    {
        European = 0x50,
    }

    public class Nintendo64RomMetadata
    {
        public static int BomSwap(int a) => (a << 24) | ((a & 0xFF00) << 8) | ((a & 0xFF0000) >> 8) | (a >> 24);
        public static uint BomSwap(uint a) => (a << 24) | ((a & 0xFF00) << 8) | ((a & 0xFF0000) >> 8) | (a >> 24);

        public int ClockRate
        {
            get
            {
                return BomSwap(BitConverter.ToInt32(RawRom, 4));
            }
            set
            {
                byte[] data = BitConverter.GetBytes(BomSwap(value));
                Buffer.BlockCopy(data, 0, RawRom, 4, data.Length);
            }
        }
        public uint EntryPoint
        {
            get
            {
                return BomSwap(BitConverter.ToUInt32(RawRom, 8));
            }
            set
            {
                byte[] data = BitConverter.GetBytes(BomSwap(value));
                Buffer.BlockCopy(data, 0, RawRom, 8, data.Length);
            }
        }
        public int ReleaseOffset
        {
            get
            {
                return BomSwap(BitConverter.ToInt32(RawRom, 0xc));
            }
            set
            {
                byte[] data = BitConverter.GetBytes(BomSwap(value));
                Buffer.BlockCopy(data, 0, RawRom, 0xc, data.Length);
            }
        }
        public uint CRC1
        {
            get
            {
                return BomSwap(BitConverter.ToUInt32(RawRom, 0x10));
            }
            set
            {
                byte[] data = BitConverter.GetBytes(BomSwap(value));
                Buffer.BlockCopy(data, 0, RawRom, 0x10, data.Length);
            }
        }
        public uint CRC2
        {
            get
            {
                return BomSwap(BitConverter.ToUInt32(RawRom, 0x14));
            }
            set
            {
                byte[] data = BitConverter.GetBytes(BomSwap(value));
                Buffer.BlockCopy(data, 0, RawRom, 0x14, data.Length);
            }
        }
        public string Name
        {
            get
            {
                return Encoding.ASCII.GetString(RawRom, 0x20, 0x14).TrimEnd(' ');
            }
            set
            {
                byte[] data = Encoding.ASCII.GetBytes(value.ToArray());
                for (int i = 0x20; i < 0x20 + 0x14; i++) RawRom[i] = (byte)' ';
                Buffer.BlockCopy(data, 0, RawRom, 0x20, Math.Min(0x14, data.Length));
            }
        }
        public string Developer
        {
            get
            {
                return BitConverter.ToChar(RawRom, 0x3B).ToString();
            }
            set
            {
                if (value != null && value.Length > 0)
                    RawRom[0x3B] = (byte)value[0];
            }
        }
        public string CartID
        {
            get
            {
                return Encoding.ASCII.GetString(RawRom, 0x3C, 2);
            }
            set
            {
                byte[] data = Encoding.ASCII.GetBytes(value);
                if (data.Length == 2)
                    Buffer.BlockCopy(data, 0, RawRom, 0x3C, data.Length);
            }
        }
        public byte CountryCode
        {
            get
            {
                return RawRom[0x3E];
            }
            set
            {
                RawRom[0x3E] = (byte)value;
            }
        }
        public byte Version
        {
            get
            {
                return RawRom[0x3F];
            }
            set
            {
                RawRom[0x3F] = value;
            }
        }
        public byte[] BootStrap
        {
            get
            {
                byte[] data = new byte[0xFC0];
                Buffer.BlockCopy(RawRom, 0x40, data, 0, data.Length);
                return data;
            }
            set
            {
                if (value.Length == 0xFC0)
                    Buffer.BlockCopy(value, 0, RawRom, 0x40, value.Length);
            }
        }

        public byte[] RawRom { get; set; }

        public Nintendo64RomMetadata(string file) : this(File.ReadAllBytes(file))
        {

        }
        public Nintendo64RomMetadata(byte[] data)
        {
            if (data.Length < 0x1000 || data.Length % 4 != 0)
                throw new Exception("Invalid n64 ROM Size");

            //check for endian swap
            if (data[0] != 0x80 && data[1] == 0x80)
            {
                RawRom = new byte[data.Length];
                for (int i = 0; i < data.Length; i += 2)
                {
                    RawRom[i + 0] = data[i + 1];
                    RawRom[i + 1] = data[i + 0];
                }
            }
            else RawRom = data;
        }
    }

}
