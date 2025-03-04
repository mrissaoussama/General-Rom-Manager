using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Organizers;
using RomManagerShared.PSVita.Parsers;
using RomManagerShared.Utils;

namespace RomManagerShared.PSVita;

public class PSVitaManager : ConsoleManager<PSVitaConsole>, IHasRomLicenses
{
    public PSVitaManager(
RomParserExecutor<PSVitaConsole> romParserExecutor)
: base(romParserExecutor)
    {
    }

    public List<License> RomLicenses { get; set; } = [];

    public Task<List<License>> GetLicensesRecursive(string path)
    {
       var rifPaths= FileUtils.GetFilesInDirectoryWithExtensions(path, new List<string> { ".rif" });
       PSVitaRomOrganizer pSVitaRomOrganizer = new PSVitaRomOrganizer();
        foreach (var rifPath in rifPaths)
        {
            PSVitaLicense license = new PSVitaLicense(rifPath);
       
            RomLicenses.Add(license);
        }
        pSVitaRomOrganizer.OrganizeLicenses(RomLicenses, RomList);
        return Task.FromResult(RomLicenses);


    }

    public Task<bool> HasMissingLicenses(List<Rom> roms)
    {
        //check if number of licences with the same title id is equal to the number of roms with the same title id
        foreach (var rom in roms)
        {
            var licenses = RomLicenses.Where(x => x.TitleID == rom.TitleID).ToList();
            if (licenses.Count < roms.Count(x => x.TitleID == rom.TitleID))
            {
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }
}
