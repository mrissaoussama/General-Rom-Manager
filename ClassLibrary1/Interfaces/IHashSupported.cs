using RomManagerShared.Base;

namespace RomManagerShared.Interfaces;

public interface IHasExternalHashSource
{
    Task<IEnumerable<RomHash>> GetRomHashesFromExternalSource(Rom rom);
    Task<IEnumerable<RomHash>> CompareHashesToExternalDatabase(Rom rom);

}
