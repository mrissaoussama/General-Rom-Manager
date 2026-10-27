using RomManagerShared.Base;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;
using RomManagerShared.WiiU.Configuration;
using System.Diagnostics;

namespace RomManagerShared.WiiU.Parsers;

public class WiiUTMDTIKParser : IRomParser<WiiUConsole>
{
    public List<string> Extensions { get; set; }

    public WiiUTMDTIKParser()
    {
        Extensions = ["tmd"];
    }

    public async Task<List<Rom>> ProcessFile(string path)
    {
        var tmd = WiiUFormats.ReadTmd(path);
        if (tmd is null)
            return [];

        string directory = Path.GetDirectoryName(path)!;
        var metadir = Path.Combine(directory, "meta");
        var appdir = Path.Combine(directory, "code");
        bool metaExisted = Directory.Exists(metadir);
        bool appExisted = Directory.Exists(appdir);

        // Optional: a user-supplied cdecrypt extracts meta/code for names and icons. It writes into the
        // title folder, so only folders it created are removed afterwards.
        bool decrypted = false;
        if (!(metaExisted && appExisted) && WiiUConfiguration.AllowCdecrypt()
            && File.Exists(Path.Combine(directory, "title.tik")))
        {
            var tool = WiiUConfiguration.GetCdecryptPath();
            if (File.Exists(tool))
            {
                ProcessStartInfo psi = new()
                {
                    FileName = tool,
                    Arguments = $"\"{path}\"",
                    WorkingDirectory = directory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using Process process = Process.Start(psi)!;
                await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                decrypted = true;
            }
            else
            {
                FileUtils.Log($"[WiiU] cdecrypt not found at '{tool}'");
            }
        }

        Rom rom = new();
        var appXml = Path.Combine(appdir, "app.xml");
        if (File.Exists(appXml))
            rom = WiiUUtils.ParseAppXml(await File.ReadAllTextAsync(appXml), rom);

        var metaXml = Path.Combine(metadir, "meta.xml");
        if (File.Exists(metaXml))
        {
            rom = WiiUUtils.ParseMetaXml(await File.ReadAllTextAsync(metaXml), rom);
            // Icons only survive when the folder was already there.
            if (!decrypted || metaExisted)
                rom.AddImages(new[] { "iconTex.tga", "bootTvTex.tga", "bootDrcTex.tga" }
                    .Select(name => Path.Combine(metadir, name))
                    .Where(File.Exists));
        }

        // The TMD is authoritative for ID and version.
        rom.TitleID = tmd.Value.TitleId;
        rom.Version = tmd.Value.Version.ToString();
        var metadataClass = WiiUUtils.GetRomMetadataClass(rom.TitleID) ?? typeof(WiiUGame);
        if (metadataClass != rom.GetType())
        {
            var typed = (Rom)Activator.CreateInstance(metadataClass)!;
            typed.MergeFrom(rom);
            rom = typed;
        }

        if (decrypted)
        {
            if (!metaExisted && Directory.Exists(metadir))
                Directory.Delete(metadir, true);
            if (!appExisted && Directory.Exists(appdir))
                Directory.Delete(appdir, true);
        }
        rom.IsFolderFormat = true;
        rom.Path = path;
        return [rom];
    }
}
