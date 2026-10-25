using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using LibHac.Tools.Es;
using LibHac.Spl;
using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
namespace RomManagerShared.Switch.Parsers;

public class SwitchRomNSPXCIParser : IRomParser<SwitchConsole>, IHasRequirements
{
    public List<string> Extensions { get; set; }

    // ── Requirements ──────────────────────────────────────────────────────
    private static readonly string DefaultProdKeys =
        Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.switch\prod.keys");
    private static readonly string DefaultTitleKeys =
        Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.switch\title.keys");

    private readonly ConsoleRequirement _prodKeysReq = new()
    {
        Id          = "switch.prod_keys",
        DisplayName = "prod.keys",
        Description = "Nintendo Switch production keys file required for NSP/XCI decryption.",
        Kind        = RequirementKind.KeyFile,
        DefaultValue = DefaultProdKeys,
        IsOptional  = false,
    };
    private readonly ConsoleRequirement _titleKeysReq = new()
    {
        Id          = "switch.title_keys",
        DisplayName = "title.keys",
        Description = "Per-title decryption keys (optional but recommended).",
        Kind        = RequirementKind.KeyFile,
        DefaultValue = DefaultTitleKeys,
        IsOptional  = true,
    };

    public IReadOnlyList<ConsoleRequirement> GetRequirements() => [_prodKeysReq, _titleKeysReq];

    // ── Lazy keyset ───────────────────────────────────────────────────────
    private KeySet? _keyset;
    private bool _keysetInitAttempted;

    private KeySet? EnsureKeyset()
    {
        if (_keysetInitAttempted) return _keyset;
        _keysetInitAttempted = true;

        var prodPath  = _prodKeysReq.EffectiveValue;
        var titlePath = _titleKeysReq.EffectiveValue;

        if (string.IsNullOrEmpty(prodPath) || !File.Exists(prodPath))
        {
            FileUtils.Log($"[SwitchNSPXCI] prod.keys not found at '{prodPath}' — parser disabled.");
            return null;
        }

        try
        {
            _keyset = new KeySet();
            ExternalKeyReader.ReadKeyFile(_keyset,
                titleKeysFilename: File.Exists(titlePath) ? titlePath : null,
                prodKeysFilename: prodPath);
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[SwitchNSPXCI] Failed to load keys: {ex.Message}");
            _keyset = null;
        }
        return _keyset;
    }

    // ── Fields ────────────────────────────────────────────────────────────
    List<Rom> RomList;
    PartitionFileSystem? NSPPartitionFileSystem;
    XciPartition? XCIPartitionFileSystem;
    LocalStorage? localStorage;

    public SwitchRomNSPXCIParser()
    {
        Extensions = ["xci", "nsp"];
        RomList = [];
    }

    public async Task<List<Rom>> ProcessFile(string path)
    {
        // Abort early if keys are unavailable
        var keyset = EnsureKeyset();
        if (keyset is null)
        {
            FileUtils.Log($"[SwitchNSPXCI] Skipping '{System.IO.Path.GetFileName(path)}' — no valid keyset.");
            return [];
        }

        List<Rom> roms = [];
        try
        {
            localStorage = new LocalStorage(path, FileAccess.Read);
            await FillRomList(path, roms, keyset);
        }
        catch (Exception ex)
        {
            localStorage?.Dispose();
            NSPPartitionFileSystem?.Dispose();
            XCIPartitionFileSystem?.Dispose();
            FileUtils.Log($"exception error {path} {ex.Message}");
        }
        if (RomList is null)
        {
            FileUtils.Log($"null error {path}");
            RomList = [];
        }
        RemoveFileLock();
        RomList.AddRange(roms);
        return roms;
    }
    private Task FillRomList(string path, List<Rom> roms, KeySet keyset)
    {
        IEnumerable<DirectoryEntryEx> entries = [];
        SwitchFs switchFs;
        if (IsNSP(path))
        {
            ParseNSP();
            ImportTickets(NSPPartitionFileSystem!, keyset);
            switchFs = SwitchFs.OpenNcaDirectory(keyset, NSPPartitionFileSystem!);
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

                        roms.Add(dlcMetaData);
                    }
                }
            }
        }
        else
        {
            ParseXCI(keyset);
            entries = XCIPartitionFileSystem!.EnumerateEntries();
            foreach (var entry in entries)
            {
                if (entry.Name.EndsWith(".tik"))
                {
                    string titleid = entry.Name[..16];
                    var romtype = SwitchUtils.GetRomMetadataClass(titleid);
                    var rom = (Rom)Activator.CreateInstance(romtype);
                    rom.TitleID = titleid.ToUpper();
                    rom.Path = path;
                    if (roms.FirstOrDefault(x => x.TitleID == rom.TitleID) is null)
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
                   // Console.WriteLine($"{id} |headersig |ncaverify {ncv} section {secv} ||vsmh{vsm} ");
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
    private void ParseXCI(KeySet keyset)
    {
        var xci = new Xci(keyset, localStorage!);
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
        NSPPartitionFileSystem.Initialize(localStorage!);
    }
    // Titlekey-encrypted NSPs carry their key in a .tik; without it every NCA fails to open.
    private static void ImportTickets(IFileSystem fs, KeySet keyset)
    {
        foreach (var entry in fs.EnumerateEntries("/", "*.tik"))
        {
            try
            {
                using var tikFile = new UniqueRef<IFile>();
                fs.OpenFile(ref tikFile.Ref, entry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();
                var ticket = new Ticket(tikFile.Get.AsStream());
                var titleKey = ticket.GetTitleKey(keyset);
                if (titleKey is null) continue;
                keyset.ExternalKeySet.Add(new RightsId(ticket.RightsId), new AccessKey(titleKey));
            }
            catch (Exception ex)
            {
                FileUtils.Log($"[SwitchNSPXCI] Bad ticket {entry.FullPath}: {ex.Message}");
            }
        }
    }
    public void RemoveFileLock()
    {
        localStorage?.Dispose();
        NSPPartitionFileSystem?.Dispose();
        XCIPartitionFileSystem?.Dispose();
    }
}
