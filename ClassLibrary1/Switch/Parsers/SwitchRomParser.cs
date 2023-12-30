using RomManagerShared;
using LibHac;
using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Ns;
using LibHac.Sdmmc;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.RomFs;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace RomManagerShared.Switch.Parsers
{
    public class SwitchRomParser : IRomParser
    {
        public string prodKeys;
        public string titleKeys; KeySet keyset;
        List<IRom> RomList;
        string switchRomPath;
        PartitionFileSystem NSPPartitionFileSystem;
        XciPartition XCIPartitionFileSystem;
        LocalStorage localStorage;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SwitchRomParser()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
       {
            prodKeys = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.switch\prod.keys");
            titleKeys = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.switch\title.keys");
            keyset = new KeySet();
            ExternalKeyReader.ReadKeyFile(keyset, titleKeysFilename: titleKeys, prodKeysFilename: prodKeys); ;

        }
        public async Task<List<IRom>> ProcessFile(string switchRomPath)
        {
            this.switchRomPath = switchRomPath;
            RomList = new List<IRom>();
            try
            {
                localStorage = new LocalStorage(switchRomPath, FileAccess.Read);
                if (IsNSP())
                {
                    ParseNSP();
                }
                else if (IsXCI())
                {
                    ParseXCI();
                }
                await EnumerateRomList();
            }
            catch (Exception ex)
            {
                localStorage.Dispose();
                if (NSPPartitionFileSystem is not null)
                    NSPPartitionFileSystem.Dispose();
                if (XCIPartitionFileSystem is not null)
                    XCIPartitionFileSystem.Dispose();
                Console.WriteLine($"null error {switchRomPath} {ex.Message}");
                throw;

            }
            if (RomList is null)
            {
                Console.WriteLine($"null error {switchRomPath}");
            }
            RemoveFileLock();
            return RomList;

        }

        private Task EnumerateRomList()
        {
            List<DirectoryEntryEx> entries = new List<DirectoryEntryEx>();
            SwitchFs switchFs;
            if (NSPPartitionFileSystem is not null)
            {
                switchFs = SwitchFs.OpenNcaDirectory(keyset, NSPPartitionFileSystem);

            }
            else
            {
                switchFs = SwitchFs.OpenNcaDirectory(keyset, XCIPartitionFileSystem);
            }
            foreach (Application app in switchFs.Applications.Values.OrderBy(x => x.Name))
            {

                if (app.Main != null)
                {
                    var gameMetaData = new SwitchGameMetaData();
                    gameMetaData.TitleName = app.Main.Name;
                    gameMetaData.TitleID = app.Main.Id.ToString("X16");
                    gameMetaData.Size = app.Main.GetSize();
                    gameMetaData.Version = app.Main.Version.Version.ToString();
                    gameMetaData.MinimumFirmware = app.Main.Metadata?.MinimumSystemVersion?.Version.ToString();
                    gameMetaData.Path = switchRomPath;
                    RomList.Add(gameMetaData);
                }
                if (app.Patch != null)
                {
                    var updateMetaData = new SwitchUpdateMetaData();
                    updateMetaData.TitleID = app.Patch.Id.ToString("X16");
                    updateMetaData.Size = app.Patch.GetSize();
                    updateMetaData.Version = app.Patch.Version.Version.ToString();
                    updateMetaData.TitleName = app.Patch.Name + $" Update {updateMetaData.Version}";
                    updateMetaData.MinimumFirmware = app.Patch.Metadata?.MinimumSystemVersion?.Version.ToString();
                    updateMetaData.Path = switchRomPath;

                    RomList.Add(updateMetaData);
                }

                if (app.AddOnContent.Count > 0)
                {
                    int dlcIndex = 0;
                    foreach (var addOnContent in app.AddOnContent)
                    {
                        dlcIndex++;
                        var dlcMetaData = new SwitchUpdateMetaData();
                        dlcMetaData.TitleID = addOnContent.Id.ToString("X16");
                        dlcMetaData.Size = addOnContent.GetSize();
                        dlcMetaData.Version = addOnContent.Version.Version.ToString();
                        dlcMetaData.MinimumFirmware = addOnContent.Metadata?.MinimumSystemVersion?.Version.ToString();
                        dlcMetaData.Path = switchRomPath;

                        if (addOnContent.Name != null)
                        {
                            dlcMetaData.TitleName = addOnContent.Name;
                        }
                        else
                        {
                            dlcMetaData.TitleName = $"[{dlcMetaData.TitleID}] DLC {dlcIndex}";
                        }
                        RomList.Add(dlcMetaData);

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
        }

        private bool IsXCI()
        {
            string extension = System.IO.Path.GetExtension(switchRomPath);
            return string.Equals(extension, ".xci", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsNSP()
        {
            string extension = System.IO.Path.GetExtension(switchRomPath);
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
            if (NSPPartitionFileSystem is not null)
                NSPPartitionFileSystem.Dispose();
            if (XCIPartitionFileSystem is not null)
                XCIPartitionFileSystem.Dispose();
        }
    }
}
