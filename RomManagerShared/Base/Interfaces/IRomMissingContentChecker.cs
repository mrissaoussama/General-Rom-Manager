using RomManagerShared.Base;
namespace RomManagerShared.Interfaces;

public interface IRomMissingContentChecker
{
    List<List<Rom>> GroupedRomList { get; set; }

    Task<List<RomMissingUpdates>> GetMissingUpdates();
    Task<RomMissingUpdates?> GetMissingUpdates(Rom rom);
    Task<List<RomMissingDLCs>> GetMissingDLC();
    Task<RomMissingDLCs> GetMissingDLC(Rom rom);
    List<Rom> GetRomGroup(Rom rom);
    void LoadGroupRomList();
}
