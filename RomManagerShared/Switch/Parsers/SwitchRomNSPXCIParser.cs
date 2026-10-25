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
    private readonly object _keysetLock = new(); // files are parsed concurrently

    private KeySet? EnsureKeyset()
    {
        lock (_keysetLock)
        {
            if (_keysetInitAttempted) return _keyset;
            _keyset = LoadKeyset();
            _keysetInitAttempted = true;
            return _keyset;
        }
    }

    private KeySet? LoadKeyset()
    {
        var prodPath  = _prodKeysReq.EffectiveValue;
        var titlePath = _titleKeysReq.EffectiveValue;

        if (string.IsNullOrEmpty(prodPath) || !File.Exists(prodPath))
        {
            FileUtils.Log($"[SwitchNSPXCI] prod.keys not found at '{prodPath}' — parser disabled.");
            return null;
        }

        try
        {
            var keyset = new KeySet();
            ExternalKeyReader.ReadKeyFile(keyset,
                titleKeysFilename: File.Exists(titlePath) ? titlePath : null,
                prodKeysFilename: prodPath);
            return keyset;
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[SwitchNSPXCI] Failed to load keys: {ex.Message}");
            return null;
        }
    }

    public SwitchRomNSPXCIParser()
    {
        Extensions = ["xci", "nsp", "nsz"];
    }

    public Task<List<Rom>> ProcessFile(string path)
    {
        var keyset = EnsureKeyset();
        if (keyset is null)
        {
            FileUtils.Log($"[SwitchNSPXCI] Skipping '{System.IO.Path.GetFileName(path)}' — no valid keyset.");
            return Task.FromResult<List<Rom>>([]);
        }

        List<Rom> roms = [];
        try
        {
            using var storage = new LocalStorage(path, FileAccess.Read);
            using IFileSystem? fs = IsXCI(path) ? OpenXciSecure(storage, keyset) : OpenNsp(storage, keyset);
            if (fs is not null)
            {
                AddApplications(SwitchFs.OpenNcaDirectory(keyset, fs), path, roms);
                if (roms.Count == 0)
                    AddIdsFromNcaHeaders(fs, keyset, path, roms);
            }
            // NSZ program data is in .ncz files LibHac doesn't count; the file size is closer to the truth.
            if (path.EndsWith(".nsz", StringComparison.OrdinalIgnoreCase))
                foreach (var rom in roms) rom.Size = FileUtils.GetFileSize(path);
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[SwitchNSPXCI] {path}: {ex.Message}");
        }
        return Task.FromResult(roms);
    }

    private static PartitionFileSystem OpenNsp(IStorage storage, KeySet keyset)
    {
        var pfs = new PartitionFileSystem();
        pfs.Initialize(storage).ThrowIfFailure();
        ImportTickets(pfs, keyset);
        return pfs;
    }

    private static XciPartition? OpenXciSecure(IStorage storage, KeySet keyset)
    {
        var xci = new Xci(keyset, storage);
        return xci.HasPartition(XciPartitionType.Secure) ? xci.OpenPartition(XciPartitionType.Secure) : null;
    }

    private static void AddApplications(SwitchFs switchFs, string path, List<Rom> roms)
    {
        foreach (LibHac.Tools.Fs.Application app in switchFs.Applications.Values.OrderBy(x => x.Name))
        {
            if (app.Main != null)
                roms.Add(Fill(new SwitchGame(), app.Main, path));
            if (app.Patch != null)
                roms.Add(Fill(new SwitchUpdate(), app.Patch, path));
            foreach (var addOnContent in app.AddOnContent)
                roms.Add(Fill(new SwitchDLC(), addOnContent, path));
        }
    }

    private static Rom Fill(Rom rom, Title title, string path)
    {
        rom.TitleID = title.Id.ToString("X16");
        rom.Size = title.GetSize();
        rom.Version = title.Version.Version.ToString();
        rom.MinimumFirmware = title.Metadata?.MinimumSystemVersion?.Version.ToString();
        rom.Path = path;
        if (!string.IsNullOrEmpty(title.Name))
            rom.AddTitleName(title.Name);
        if (rom is not SwitchDLC)
            rom.Icon = IconCache.Save("Switch", rom.TitleID, ReadIcon(title), ".jpg");
        return rom;
    }

    // Preferred icon languages; any other icon_*.dat is used as a last resort.
    private static readonly string[] IconLanguages = ["AmericanEnglish", "BritishEnglish", "Japanese"];

    // The control NCA romfs holds icon_<Language>.dat (JPEG) next to control.nacp; only that file is read.
    private static byte[]? ReadIcon(Title title)
    {
        if (title.ControlNca is null) return null;
        try
        {
            using var romfs = title.ControlNca.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.None);
            var names = romfs.EnumerateEntries("/", "icon_*.dat").Select(e => e.Name).ToList();
            var name = IconLanguages.Select(l => $"icon_{l}.dat").FirstOrDefault(names.Contains) ?? names.FirstOrDefault();
            if (name is null) return null;
            using var file = new UniqueRef<IFile>();
            romfs.OpenFile(ref file.Ref, ("/" + name).ToU8Span(), OpenMode.Read).ThrowIfFailure();
            file.Get.GetSize(out long size).ThrowIfFailure();
            if (size is <= 0 or > 1024 * 1024) return null;
            var data = new byte[size];
            file.Get.Read(out _, 0, data).ThrowIfFailure();
            return data;
        }
        catch (Exception ex)
        {
            FileUtils.Log($"[SwitchNSPXCI] icon {title.Id:X16}: {ex.Message}");
            return null;
        }
    }

    // Fallback when metadata can't be decrypted: title IDs from NCA headers only (header key suffices).
    private static void AddIdsFromNcaHeaders(IFileSystem fs, KeySet keyset, string path, List<Rom> roms)
    {
        foreach (DirectoryEntryEx entry in fs.EnumerateEntries("/", "*.nca"))
        {
            try
            {
                using var ncaFile = new UniqueRef<IFile>();
                fs.OpenFile(ref ncaFile.Ref, entry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();
                var id = new Nca(keyset, ncaFile.Get.AsStorage()).Header.TitleId.ToString("X16");
                if (roms.Any(x => x.TitleID == id))
                    continue;
                var rom = (Rom)Activator.CreateInstance(SwitchUtils.GetRomMetadataClass(id))!;
                rom.TitleID = id;
                rom.Path = path;
                roms.Add(rom);
            }
            catch (Exception ex)
            {
                FileUtils.Log($"[SwitchNSPXCI] {path} {entry.Name}: {ex.Message}");
            }
        }
    }

    private static bool IsXCI(string path)
    {
        string extension = System.IO.Path.GetExtension(path);
        return string.Equals(extension, ".xci", StringComparison.OrdinalIgnoreCase);
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
}
