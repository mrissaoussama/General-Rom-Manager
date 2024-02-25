using RomManagerShared.Base;
using RomManagerShared.Interfaces;

namespace RomManagerShared.Switch.TitleInfoProviders;

public interface IUpdateVersionProvider<T> where T : GamingConsole
{
    public string Source { get; set; }    Task<Update> GetLatestVersion(string titleId);
    Task LoadVersionDatabaseAsync();
}