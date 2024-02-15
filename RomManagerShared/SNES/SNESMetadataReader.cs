using System.Text;namespace RomManagerShared.SNES;
//source https://github.com/Zeokat/SNES-ROM-Header-Dumper-CSharp/blob/master/snes_dumper.cs
public class SNESMetadataReader
{
    bool SmcHeader;
    public byte[] Data;
    SNESMetadata snesmetadata;
    int HeaderLocation;
    //i'm still not sure about the header and icon extraction, will do this later.
    public SNESMetadata GetMetadata(string path)
    {
        //reading the entire file since snes games are generally small
        Data = File.ReadAllBytes(path);
        snesmetadata = new SNESMetadata();
        SmcHeader = Data.Length % 1024 == 512 || (Data.Length % 1024 == 0 ? false : throw new Exception("invalid snes rom"));
        HeaderLocation = 0x81C0;        if (!HeaderIsAt(0x07FC0))
        {
            HeaderIsAt(0x0FFC0);
        }        ReadHeader();
        return snesmetadata;
    }
    private bool VerifyChecksum()
    {
        // La rom tiene header smc
        if (SmcHeader)
            this.HeaderLocation += 512;        snesmetadata.ChecksumCompliment = BitConverter.ToUInt16(this.Get(0x1C, 0x1D), 0);
        snesmetadata.Checksum = BitConverter.ToUInt16(this.Get(0x1E, 0x1F), 0);
        ushort ver = (ushort)(snesmetadata.Checksum ^ snesmetadata.ChecksumCompliment);
        return ver == 0xFFFF;
    }
    private bool HeaderIsAt(ushort addr)
    {
        this.HeaderLocation = addr;
        return VerifyChecksum();
    }    private void ReadHeader()
    {
        snesmetadata.Name = Encoding.ASCII.GetString(this.Get(0x00, 0x14)); // 21 chars
        snesmetadata.Layout = this.At(0x15);
        snesmetadata.CartridgeType = this.At(0x16);
        snesmetadata.RomSize = this.At(0x17);
        snesmetadata.RamSize = this.At(0x18);
        snesmetadata.CountryCode = this.At(0x19);
        snesmetadata.LicenseCode = this.At(0x1A);
        snesmetadata.VersionNumber = this.At(0x1B);
    }    private string GetROmB()
    {
        return String.Format("{0}", snesmetadata.RomSize);
    }
    private byte[] Get(int from, int to)
    {
        return this.Data.Skip(this.HeaderLocation + from).Take(to - from + 1).ToArray();
    }
    private byte At(int addr)
    {
        return this.Data[this.HeaderLocation + addr];
    }
}
