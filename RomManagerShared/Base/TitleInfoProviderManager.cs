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

    /// <summary>Combined <see cref="IHasDataFingerprint"/> of the providers; empty when none depend on data files.</summary>
    public string DataFingerprint => string.Join("|", TitleInfoProviders.OfType<IHasDataFingerprint>()
        .Select(p => p.DataFingerprint).Where(f => f.Length > 0));
    public Task<Rom> GetTitleInfo(Rom rom) => GetTitleInfo(rom, CancellationToken.None);

    public async Task<Rom> GetTitleInfo(Rom rom, CancellationToken ct)
    {
        // Providers either mutate in place or return a new (possibly re-typed) instance; keep whichever comes back.
        foreach (var titleInfo in TitleInfoProviders)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                rom = await titleInfo.GetTitleInfo(rom, ct) ?? rom;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                AppLog.Warn($"[{titleInfo.GetType().Name}] {rom.TitleID}: {ex.Message}");
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
