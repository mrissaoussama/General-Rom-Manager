using RomManagerShared.Interfaces;
using RomManagerShared.Utils;

namespace RomManagerShared.Base;

public class TitleInfoProviderManager<T> where T : GamingConsole
{
    public TitleInfoProviderManager(IEnumerable<ITitleInfoProvider<T>> titleInfoProviders)
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
    public IEnumerable<ITitleInfoProvider<T>> TitleInfoProviders { get; set; }
    public async Task<Rom> GetTitleInfo(Rom rom)
    {
        // Run all providers in sequence. Each provider may either mutate the rom
        // in-place OR return a new enriched instance. We handle both patterns:
        //   • New instance returned  → copy non-null fields onto 'rom' and continue
        //   • Same instance returned → fields were mutated in-place, continue
        foreach (var titleInfo in TitleInfoProviders)
        {
            var result = await titleInfo.GetTitleInfo(rom);
            if (!ReferenceEquals(result, rom))
            {
                // Provider returned a new instance — merge enriched data into 'rom'
                RomUtils.CopyNonNullProperties(result, rom);
            }
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
