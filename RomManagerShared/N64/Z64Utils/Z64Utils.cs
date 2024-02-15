using RomManagerShared.Base;
using System.Text;
namespace RomManagerShared.Nintendo64.Z64Utils;


public class Z64Utils
{
    public static Nintendo64Game ParseRom(string filePath)
    {
        Nintendo64RomMetadata n64Rom = new(filePath);
        Nintendo64Game game = new()
        {
            TitleID = n64Rom.CartID
       ,
            Path = filePath,
            Version = n64Rom.Version.ToString(),
            Developer = n64Rom.Developer
        };
        game.AddRegion(GetRegion(n64Rom.CountryCode));

        game.AddTitleName(n64Rom.Name);
        return game;
    }

    private static Region GetRegion(byte countryCode)
    {
        return countryCode switch
        {
            0x41 => Region.Asia,
            0x42 => Region.Brazil,
            0x43 => Region.China,
            0x44 => Region.Germany,
            0x45 => Region.USA,
            0x46 => Region.France,
            //case 0x47: return "Gateway 64 (NTSC)";
            0x48 => Region.Netherlands,
            0x49 => Region.Italy,
            0x4A => Region.Japan,
            0x4B => Region.Korea,
            //  case 0x4C: return "Gateway 64 (PAL)";
            0x4E => Region.Canada,
            0x50 => Region.Europe,
            0x53 => Region.Spain,
            0x55 => Region.Australia,
            0x57 => Region.Scandinavia,
            0x58 => Region.Europe,
            0x59 => Region.Europe,
            _ => Region.Unknown,
        };
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
            RawRom[0x3E] = value;
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
