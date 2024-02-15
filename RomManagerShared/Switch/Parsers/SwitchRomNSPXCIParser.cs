using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using RomManagerShared.Base;
namespace RomManagerShared.Switch.Parsers;

public class SwitchRomNSPXCIParser : IRomParser
{
    public HashSet<string> Extensions { get; set; }
    public string prodKeys;
    public string titleKeys; readonly KeySet keyset;
    HashSet<Rom> RomList;
    PartitionFileSystem NSPPartitionFileSystem;
    XciPartition XCIPartitionFileSystem;
    LocalStorage localStorage;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public SwitchRomNSPXCIParser()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        Extensions = ["xci", "nsp"];        prodKeys = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.switch\prod.keys");
        titleKeys = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.switch\title.keys");
        keyset = new KeySet();
        ExternalKeyReader.ReadKeyFile(keyset, titleKeysFilename: titleKeys, prodKeysFilename: prodKeys); ;        RomList = [];    }
    public async Task<HashSet<Rom>> ProcessFile(string path)
    {
        HashSet<Rom> roms = [];
        try
        {
            localStorage = new LocalStorage(path, FileAccess.Read);
            await FillRomList(path, roms);
        }
        catch (Exception ex)
        {
            localStorage.Dispose();
            NSPPartitionFileSystem?.Dispose();
            XCIPartitionFileSystem?.Dispose();
            Console.WriteLine($"exception error {path} {ex.Message}");
        }
        if (RomList is null)
        {
            Console.WriteLine($"null error {path}");
            RomList = [];
        }
        RemoveFileLock();
        RomList.UnionWith(roms);
        return roms;    }    private Task FillRomList(string path, HashSet<Rom> roms)
    {
        IEnumerable<DirectoryEntryEx> entries = [];
        SwitchFs switchFs;
        if (IsNSP(path))
        {
            ParseNSP();            switchFs = SwitchFs.OpenNcaDirectory(keyset, NSPPartitionFileSystem);
            foreach (LibHac.Tools.Fs.Application app in switchFs.Applications.Values.OrderBy(x => x.Name))
            {
                if (app.Main != null)
                {
                    var gameMetaData = new SwitchGame();
                    gameMetaData.AddTitleName(app.Main.Name);
                    gameMetaData.TitleID = app.Main.Id.ToString("X16");
                    gameMetaData.Size = app.Main.GetSize();
                    gameMetaData.Version = app.Main.Version.Version.ToString();
                    gameMetaData.MinimumFirmware = app.Main.Metadata?.MinimumSystemVersion?.Version.ToString();
                    gameMetaData.Path = path;
                    roms.Add(gameMetaData);
                }
                if (app.Patch != null)
                {
                    var updateMetaData = new SwitchUpdate
                    {
                        TitleID = app.Patch.Id.ToString("X16"),
                        Size = app.Patch.GetSize(),
                        Version = app.Patch.Version.Version.ToString()
                    };
                    updateMetaData.AddTitleName(app.Patch.Name);
                    updateMetaData.MinimumFirmware = app.Patch.Metadata?.MinimumSystemVersion?.Version.ToString();
                    updateMetaData.Path = path;
                    roms.Add(updateMetaData);
                }
                if (app.AddOnContent.Count > 0)
                {
                    int dlcIndex = 0;
                    foreach (var addOnContent in app.AddOnContent)
                    {
                        dlcIndex++;
                        var dlcMetaData = new SwitchUpdate
                        {
                            TitleID = addOnContent.Id.ToString("X16"),
                            Size = addOnContent.GetSize(),
                            Version = addOnContent.Version.Version.ToString(),
                            MinimumFirmware = addOnContent.Metadata?.MinimumSystemVersion?.Version.ToString(),
                            Path = path
                        };
                        if (addOnContent.Name != null)
                        {
                            dlcMetaData.AddTitleName(addOnContent.Name);
                        }
                        else
                        {
                            dlcMetaData.AddTitleName($"[{dlcMetaData.TitleID}] DLC {dlcIndex}");
                        }
                        roms.Add(dlcMetaData);
                    }
                }
            }
        }
        else
        {
            ParseXCI();            entries = XCIPartitionFileSystem.EnumerateEntries();
            foreach (var entry in entries)
            {
                if (entry.Name.EndsWith(".tik"))
                {
                    string titleid = entry.Name[..16];
                    var romtype = SwitchUtils.GetRomMetadataClass(titleid);
                    var rom = (Rom)Activator.CreateInstance(romtype);
                    rom.TitleID = titleid.ToUpper();
                    rom.Path = path;                    if (roms.FirstOrDefault(x => x.TitleID == rom.TitleID) is null)
                        roms.Add(rom);
                }
            }

        }
        using FileStream file = new(path, FileMode.Open, FileAccess.Read);
        string extension = System.IO.Path.GetExtension(path).ToLower();
        if (extension is ".nsp" or ".pfs0" or ".xci")
        {
            IFileSystem pfs;

            if (extension == ".xci")
            {
                pfs = new Xci(keyset, file.AsStorage()).OpenPartition(XciPartitionType.Secure);
            }
            else
            {
                var pfsTemp = new PartitionFileSystem();
                pfsTemp.Initialize(file.AsStorage()).ThrowIfFailure();
                pfs = pfsTemp;
            }
            foreach (DirectoryEntryEx fileEntry in pfs.EnumerateEntries("/", "*.nca"))
            {
                using var ncaFile = new UniqueRef<IFile>();

                pfs.OpenFile(ref ncaFile.Ref, fileEntry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                Nca nca = new(keyset, ncaFile.Get.AsStorage());
                //   if (nca.Header.ContentType == NcaContentType.Meta)
                {
                    var hs = nca.VerifyHeaderSignature();
                    var ncv = nca.VerifyNca();

                    int dataIndex = Nca.GetSectionIndexFromType(NcaSectionType.Data, nca.Header.ContentType);
                    var secv = nca.VerifySection(dataIndex);
                    var vsm = nca.ValidateSectionMasterHash(dataIndex);
                    var id = nca.Header.TitleId.ToString("X16");
                    Console.WriteLine($"{id} |headersig |ncaverify {ncv} section {secv} ||vsmh{vsm} ");
                    var romtype = SwitchUtils.GetRomMetadataClass(id);
                    var rom = (Rom)Activator.CreateInstance(romtype);
                    rom.Path = path;
                    rom.TitleID = id;
                    if (roms.FirstOrDefault(x => x.TitleID == rom.TitleID) is null)
                        roms.Add(rom);
                }
            }
        }
        return Task.CompletedTask;

    }
    private void ParseXCI()
    {
        var xci = new Xci(keyset, localStorage);
        if (xci.HasPartition(XciPartitionType.Secure))
        {
            XCIPartitionFileSystem = xci.OpenPartition(XciPartitionType.Secure);
        }
        XciPartition root = xci.OpenPartition(XciPartitionType.Root);
        if (root == null)
        {
            return;
        }
    }
    private static bool IsXCI(string path)
    {
        string extension = System.IO.Path.GetExtension(path);
        return string.Equals(extension, ".xci", StringComparison.OrdinalIgnoreCase);
    }
    private static bool IsNSP(string path)
    {
        string extension = System.IO.Path.GetExtension(path);
        return string.Equals(extension, ".nsp", StringComparison.OrdinalIgnoreCase);
    }
    private void ParseNSP()
    {
        NSPPartitionFileSystem = new PartitionFileSystem();
        NSPPartitionFileSystem.Initialize(localStorage);
    }
    public void RemoveFileLock()
    {
        localStorage.Dispose();
        NSPPartitionFileSystem?.Dispose();
        XCIPartitionFileSystem?.Dispose();
    }
}
