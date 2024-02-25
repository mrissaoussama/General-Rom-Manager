using RomManagerShared.Interfaces;

namespace RomManagerShared.Base;

public class TitleInfoProviderManager<T> where T : GamingConsole
{
    public TitleInfoProviderManager(IEnumerable<TitleInfoProvider<T>> titleInfoProviders)
    {
        TitleInfoProviders = titleInfoProviders;

    }
    public async Task Setup()
    {
        if (TitleInfoProviders != null)
        {
            await Task.WhenAll(TitleInfoProviders.Select(titleInfo => titleInfo.LoadTitleDatabaseAsync()));
        }
        await Task.CompletedTask;
    }
    public IEnumerable<TitleInfoProvider<T>> TitleInfoProviders { get; set; }
    public async Task<Rom> GetTitleInfo(Rom rom)
    {
        foreach (var titleInfo in TitleInfoProviders)
        {
            var newrom = await titleInfo.GetTitleInfo(rom);
            if (newrom != rom)
                return newrom;
        }
        return rom;
    }
}
public class VersionInfoProviderManager<T> where T : GamingConsole
{
    public VersionInfoProviderManager(IEnumerable<TitleInfoProvider<T>> titleInfoProviders)
    {
        TitleInfoProviders = titleInfoProviders;

    }
    public Task Setup()
    {
        if (TitleInfoProviders != null)
        {
            return Task.WhenAll(TitleInfoProviders.Select(titleInfo => titleInfo.LoadTitleDatabaseAsync()));
        }
        return Task.CompletedTask;
    }
    public IEnumerable<TitleInfoProvider<T>> TitleInfoProviders { get; set; }
    public async Task<Rom> GetTitleInfo(Rom rom)
    {
        foreach (var titleInfo in TitleInfoProviders)
        {
            var newrom = await titleInfo.GetTitleInfo(rom);
            if (newrom != rom)
                return newrom;
        }
        return rom;
    }
}
