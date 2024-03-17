using RomManagerShared.Base;

namespace RomManagerShared.Interfaces;

public interface IUpdateVersionProvider<T> where T : GamingConsole
{
    public string Source { get; set; }    Task<Update> GetLatestVersion(string titleId);
    Task LoadVersionDatabaseAsync();
}