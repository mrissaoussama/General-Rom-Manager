namespace RomManagerShared.Base;

public interface ITitleInfoProvider
{
    public string Source { get; set; }
    public Task LoadTitleDatabaseAsync();
    Task<Rom> GetTitleInfo(Rom rom);
}