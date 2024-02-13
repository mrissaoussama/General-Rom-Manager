using RomManagerShared.Utils.ISO2GOD.Chilano.Xbox360.IO;

namespace RomManagerShared.Utils.ISO2GOD.Chilano.Xbox360.Xdbf;

public class XdbfTable : List<XdbfTableEntry>
{
    public XdbfTable(CBinaryReader b, XdbfHeader header)
    {
        b.Seek(30L, SeekOrigin.Begin);
        for (int i = 0; i < header.NumEntries; i++)
        {
            Add(new XdbfTableEntry(b));
        }
        while (b.PeekChar() == 0)
        {
            b.ReadByte();
        }
    }
}
