using RomManagerShared.Base;

namespace RomManagerShared.Interfaces;

public interface IHasRomLicenses
{
    List<License> RomLicenses { get; set; }

    Task<List<License>> GetLicensesRecursive(string path);
    Task<bool> HasMissingLicenses(List<Rom> roms);
}
