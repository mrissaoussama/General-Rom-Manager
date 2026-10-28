using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Utils;

namespace RomManagerTests;

public sealed class SaveDataTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("saves").FullName;
    private string AppData => Path.Combine(_dir, "appdata");
    private string Documents => Path.Combine(_dir, "documents");
    public void Dispose() => Directory.Delete(_dir, true);

    private string Write(string relative, string content = "save")
    {
        var path = Path.GetFullPath(Path.Combine(_dir, relative));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private IReadOnlyList<SaveLocation> Find(string console, Rom rom, string? emulator = null) =>
        SaveData.Find(console, rom, emulator, AppData, Documents);

    [Fact]
    public void FindsSaveFilesNextToTheRom()
    {
        var rom = new Game { Path = Write("roms/Pokemon.gba"), TitleID = "BPEE" };
        var sav = Write("roms/Pokemon.sav");
        Write("roms/Other.sav");

        Find("GameBoyAdvance", rom).Should().ContainSingle().Which.Path.Should().Be(sav);
    }

    [Fact]
    public void FindsCitraSavesForA3dsGame()
    {
        var zero = new string('0', 32);
        var data = Path.GetDirectoryName(Write(Path.Combine("appdata", "Citra", "sdmc", "Nintendo 3DS", zero, zero, "title", "00040000", "00030700", "data", "00000001", "save.bin")))!;

        var save = Find("ThreeDS", new Game { TitleID = "0004000000030700", Path = Write("roms/mk7.3ds") }).Should().ContainSingle().Subject;

        save.Path.Should().Be(Path.GetDirectoryName(data));
        save.IsDirectory.Should().BeTrue();
        Find("ThreeDS", new Update { TitleID = "0004000E00030700", Path = Write("roms/u.cia") }).Should().BeEmpty("updates have no saves of their own");
    }

    [Fact]
    public void FindsPortablePpssppSavesByPrefix()
    {
        var emulator = Write("emu/PPSSPPWindows64.exe");
        Write("emu/memstick/PSP/SAVEDATA/ULES00590DATA00/DATA.BIN");
        Write("emu/memstick/PSP/SAVEDATA/ULES00590SYS/PARAM.SFO");
        Write("emu/memstick/PSP/SAVEDATA/ULUS10000DATA/DATA.BIN");

        Find("PSP", new Game { TitleID = "ULES00590", Path = Write("roms/daxter.iso") }, emulator)
            .Select(s => Path.GetFileName(s.Path)).Should().BeEquivalentTo(["ULES00590DATA00", "ULES00590SYS"]);
    }

    [Fact]
    public void FindsDolphinWiiSavesByHexGameId()
    {
        Write("documents/Dolphin Emulator/Wii/title/00010000/524d4745/data/banner.bin");

        Find("Wii", new Game { TitleID = "RMGE01", Path = Write("roms/smg.iso") }).Should().ContainSingle();
    }

    [Fact]
    public void BackupAndRestore_PutFilesAndFoldersBack()
    {
        var file = Write("roms/Zelda.srm", "file v1");
        var folderFile = Write("saves/ULES00590DATA00/DATA.BIN", "folder v1");
        var folder = Path.GetDirectoryName(folderFile)!;
        var zip = Path.Combine(_dir, "backup", "zelda.zip");
        SaveData.Backup([new SaveLocation(file, false, "test"), new SaveLocation(folder, true, "test")], zip);

        File.WriteAllText(file, "file v2");
        File.WriteAllText(folderFile, "folder v2");
        Write("saves/ULES00590DATA00/NEW.BIN");

        SaveData.Contents(zip).Select(c => c.Path).Should().Equal(file, folder);
        SaveData.Restore(zip);

        File.ReadAllText(file).Should().Be("file v1");
        File.ReadAllText(folderFile).Should().Be("folder v1");
        File.Exists(Path.Combine(folder, "NEW.BIN")).Should().BeFalse("a restored folder replaces what was there");
    }

    [Fact]
    public void Restore_RefusesZipsWithoutAManifest()
    {
        var zip = Path.Combine(_dir, "other.zip");
        using (var archive = System.IO.Compression.ZipFile.Open(zip, System.IO.Compression.ZipArchiveMode.Create))
            archive.CreateEntry("x.txt");

        var act = () => SaveData.Restore(zip);

        act.Should().Throw<InvalidDataException>();
    }
}
