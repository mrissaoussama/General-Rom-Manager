using RomManagerShared.Interfaces;

namespace RomManagerShared.Base;

public interface ITitleInfoProvider<T> where T : GamingConsole
{
    public string Source { get; set; }
    public Task LoadTitleDatabaseAsync();
    Task<Rom> GetTitleInfo(Rom rom);

    /// <summary>Cancellable lookup; providers with long work (hashing disc images) override it.</summary>
    Task<Rom> GetTitleInfo(Rom rom, CancellationToken ct) => GetTitleInfo(rom);
}
public class TitleInfoProvider<T> : ITitleInfoProvider<T> where T : GamingConsole
{
    public TitleInfoProvider()
    {
        Source = string.Empty;
    }
    public TitleInfoProvider(string source)
    {
        Source = source;
    }
    public string Source { get; set; }
    public virtual  Task LoadTitleDatabaseAsync()
    {
        return Task.CompletedTask;
    }
  public virtual Task<Rom> GetTitleInfo(Rom rom)
    {
        return Task.FromResult(rom);
    }
}

