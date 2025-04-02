using RomManagerShared.Base;
using RomManagerShared.Interfaces;
namespace RomManagerShared.Utils;

// In RomManagerShared.Utils.IRomOrganizer.cs

public interface IBaseRomOrganizer
{
    string Description { get; set; }
}

public interface IRomOrganizer : IBaseRomOrganizer
{
    void Organize(string sourceDirectory, string destinationDirectory, bool removeEmptyFolders = true, bool dryRun = false);
}

public interface IRomOrganizer<T> : IBaseRomOrganizer where T : GamingConsole
{
    void Organize(string sourceDirectory, string destinationDirectory, bool removeEmptyFolders = true, bool dryRun = false);

}

public interface ILicenseOrganizer<T> where T : GamingConsole
{
    string Description { get; set; }
    void OrganizeLicenses(List<License> licenseList, List<Rom> romList);
}