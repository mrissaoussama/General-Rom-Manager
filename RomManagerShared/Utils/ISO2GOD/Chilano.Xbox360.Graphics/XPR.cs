using RomManagerShared.Utils.ISO2GOD.Chilano.Xbox360.IO;

namespace RomManagerShared.Utils.ISO2GOD.Chilano.Xbox360.Graphics;

public class XPR
{
    public XPRHeader Header;

    public byte[] Image;

    public bool IsValid
    {
        get
        {
            return Header != null && Header.IsValid;
        }
    }

    public XPRFormat Format
    {
        get
        {
            return Header == null ? XPRFormat.None : (XPRFormat)Header.TextureFormat;
        }
    }

    public int Width
    {
        get
        {
            return Header == null ? -1 : (int)Math.Pow(2.0, Header.TextureRes2);
        }
    }

    public int Height
    {
        get
        {
            return Header == null ? -1 : (int)Math.Pow(2.0, Header.TextureRes2);
        }
    }

    public XPR()
    {
    }

    public XPR(CBinaryReader br)
    {
        init(br);
    }

    public XPR(byte[] data)
    {
        init(new CBinaryReader(EndianType.LittleEndian, new MemoryStream(data)));
    }

    private void init(CBinaryReader br)
    {
        br.Seek(0L, SeekOrigin.Begin);
        Header = new XPRHeader(br);
        if (IsValid)
        {
            readImageData(br);
        }
    }

    private void readImageData(CBinaryReader br)
    {
        br.Seek(Header.HeaderSize, SeekOrigin.Begin);
        int num = (int)(Header.FileSize - Header.HeaderSize);
        Image = new byte[num];
        Image = br.ReadBytes(num);
    }

    public DDS ConvertToDDS(int Width, int Height)
    {
        DDS dDS = new(DDSType.ARGB);
        switch (Format)
        {
            case XPRFormat.DXT1:
                dDS = new DDS(DDSType.DXT1);
                break;
            case XPRFormat.ARGB:
                dDS = new DDS(DDSType.ARGB);
                break;
        }
        dDS.SetDetails(Height, Width, 1u);
        dDS.Data = Image;
        return dDS;
    }
}
