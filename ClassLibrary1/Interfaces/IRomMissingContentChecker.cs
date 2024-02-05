using RomManagerShared.Base;
namespace RomManagerShared.Interfaces
{
    public interface IRomMissingContentChecker
    {
        HashSet<HashSet<Rom>> GroupedRomList { get; set; }

        Task<HashSet<RomMissingUpdates>> GetMissingUpdates();
        Task<RomMissingUpdates?> GetMissingUpdates(Rom rom);
        Task<HashSet<RomMissingDLCs>> GetMissingDLC();
        Task<RomMissingDLCs> GetMissingDLC(Rom rom);
        HashSet<Rom> GetRomGroup(Rom rom);
        void LoadGroupRomList();
    }
}
