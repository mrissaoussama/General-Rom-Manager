namespace RomManagerShared.Utils.ISO2GOD.Chilano.Xbox360.Iso;

public class GDFStats
{
    public enum GDFSectorStatus : uint
    {
        Empty = 4278190080u,
        FileData = 4292796126u,
        DirTable = 4294901760u,
        Gdf = 4278255360u
    }

    public uint SectorSize;

    public uint MaxSectors;

    public ulong MaxBytes;

    public uint UsedDataSectors;

    public uint UsedDirSectors;

    public uint UsedGDFSectors;

    public uint TotalFiles;

    public uint TotalDirs;

    public ulong DataBytes;

    public uint FreeSectors => MaxSectors - (UsedDataSectors + UsedDirSectors + UsedGDFSectors);

    public uint UsedSectors => UsedDataSectors + UsedDirSectors + UsedGDFSectors;

    public GDFStats(GDFVolumeDescriptor volDesc)
    {
        SectorSize = volDesc.SectorSize;
        MaxSectors = volDesc.VolumeSectors;
        MaxBytes = volDesc.VolumeSize;
    }

    // The original tool drew a sector map here; RomManager only needs the counters.
    public void SetPixel(uint Sector, GDFSectorStatus Status)
    {
    }

    public override string ToString()
    {
        string text = "GDF Statistics\n---------------------------";
        text = text + "\nTotal Bytes: " + MaxBytes;
        text = text + "\nTotal Sectors: " + MaxSectors;
        text = text + "\n\nFiles: " + TotalFiles;
        text = text + "\nDirs: " + (TotalDirs - 1);
        text = text + "\nTotal Used Sectors: " + UsedSectors;
        text = text + "\nTotal Used Bytes: " + DataBytes;
        text = text + "\n\nUsed Data Sectors: " + UsedDataSectors;
        text = text + "\nUsed Dir Sectors: " + UsedDirSectors;
        text = text + "\nUsed GDF Sectors: " + UsedGDFSectors;
        return text + "\nFree Sectors: " + FreeSectors;
    }
}
