namespace RomManagerShared.Interfaces;

public interface IUpdateVersionProvider
{
    public string Source { get; set; }    Task<string> GetLatestVersion(string titleId);
    Task LoadVersionDatabaseAsync();
}