using RomManagerShared.Base;
using RomManagerShared.Interfaces;

namespace RomManagerShared.Base.Interfaces;

public interface IHasTitleProviders<T> where T : GamingConsole
{
    public IEnumerable<TitleInfoProvider<T>> TitleInfoProviders { get; set; }
    public Task<Rom> GetTitleInfo(Rom rom);
}