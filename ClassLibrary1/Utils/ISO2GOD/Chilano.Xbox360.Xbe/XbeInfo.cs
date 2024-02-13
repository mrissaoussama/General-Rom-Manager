using RomManagerShared.Utils.ISO2GOD.Chilano.Xbox360.IO;

namespace RomManagerShared.Utils.ISO2GOD.Chilano.Xbox360.Xbe;

public class XbeInfo : IDisposable
{
    private byte[] data;

    private CBinaryReader br;

    public XbeHeader Header;

    public XbeCertifcate Certifcate;

    public List<XbeSection> Sections = [];

    public bool IsValid
    {
        get
        {
            return Header != null && Header.IsValid;
        }
    }

    public XbeInfo(byte[] Xbe)
    {
        data = Xbe;
        br = new CBinaryReader(EndianType.LittleEndian, new MemoryStream(data));
        Header = new XbeHeader(br);
        if (!Header.IsValid)
        {
            return;
        }
        br.Seek(Header.CertificateAddress - Header.BaseAddress, SeekOrigin.Begin);
        Certifcate = new XbeCertifcate(br);
        br.Seek(Header.SectionHeadersAddress - Header.BaseAddress, SeekOrigin.Begin);
        for (uint num = 0u; num < Header.NumberOfSections; num++)
        {
            Sections.Add(new XbeSection(br));
        }
        foreach (XbeSection section in Sections)
        {
            section.Read(br, Header.BaseAddress);
        }
    }

    public void Dispose()
    {
        br.Close();
    }
}
