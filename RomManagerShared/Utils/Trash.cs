using System.Globalization;
using System.Runtime.InteropServices;

namespace RomManagerShared.Utils;

/// <summary>Moves files to the Recycle Bin (Windows), Trash (macOS) or the freedesktop.org trash (Linux).</summary>
public static class Trash
{
    /// <summary>True when this OS has a trash this class can use.</summary>
    public static bool IsSupported => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux();

    /// <summary>Moves <paramref name="path"/> to the trash. Throws when that is not possible; never deletes permanently.</summary>
    public static void MoveToTrash(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
            throw new FileNotFoundException("Nothing to move to the trash", path);
        if (OperatingSystem.IsWindows()) RecycleWindows(path);
        else if (OperatingSystem.IsMacOS()) MoveInto(path, Path.Combine(Home, ".Trash"));
        else if (OperatingSystem.IsLinux()) TrashFreedesktop(path, XdgDataHome);
        else throw new PlatformNotSupportedException("No trash on this platform");
    }

    private static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private static string XdgDataHome =>
        Environment.GetEnvironmentVariable("XDG_DATA_HOME") is { Length: > 0 } xdg ? xdg : Path.Combine(Home, ".local", "share");

    /// <summary>freedesktop.org Trash spec: the file goes to Trash/files, a .trashinfo records where it came from.</summary>
    public static string TrashFreedesktop(string path, string dataHome)
    {
        var trash = Path.Combine(dataHome, "Trash");
        var files = Path.Combine(trash, "files");
        var info = Path.Combine(trash, "info");
        Directory.CreateDirectory(files);
        Directory.CreateDirectory(info);

        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
        var target = UniqueName(files, name);
        var infoFile = Path.Combine(info, Path.GetFileName(target) + ".trashinfo");
        File.WriteAllText(infoFile,
            "[Trash Info]\n" +
            $"Path={Uri.EscapeDataString(Path.GetFullPath(path)).Replace("%2F", "/")}\n" +
            $"DeletionDate={DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)}\n");
        try
        {
            Move(path, target);
        }
        catch
        {
            File.Delete(infoFile);
            throw;
        }
        return target;
    }

    private static string MoveInto(string path, string trashDir)
    {
        Directory.CreateDirectory(trashDir);
        var target = UniqueName(trashDir, Path.GetFileName(Path.TrimEndingDirectorySeparator(path)));
        Move(path, target);
        return target;
    }

    private static void Move(string path, string target)
    {
        if (Directory.Exists(path)) Directory.Move(path, target);
        else File.Move(path, target);
    }

    private static string UniqueName(string dir, string name)
    {
        var candidate = Path.Combine(dir, name);
        for (int i = 2; File.Exists(candidate) || Directory.Exists(candidate); i++)
            candidate = Path.Combine(dir, $"{Path.GetFileNameWithoutExtension(name)} {i}{Path.GetExtension(name)}");
        return candidate;
    }

    // ── Windows: SHFileOperation with FOF_ALLOWUNDO sends to the Recycle Bin ──

    private const uint FO_DELETE = 0x0003;
    private const ushort FOF_SILENT = 0x0004, FOF_NOCONFIRMATION = 0x0010, FOF_ALLOWUNDO = 0x0040, FOF_NOERRORUI = 0x0400;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileOpStruct
    {
        public nint hwnd;
        public uint wFunc;
        public string pFrom;
        public string? pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
        public nint hNameMappings;
        public string? lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref ShFileOpStruct op);

    private static void RecycleWindows(string path)
    {
        // Network and most removable drives have no Recycle Bin: SHFileOperation would delete permanently.
        var full = Path.GetFullPath(path);
        var root = Path.GetPathRoot(full);
        if (string.IsNullOrEmpty(root) || full.StartsWith(@"\\", StringComparison.Ordinal)
            || new DriveInfo(root).DriveType != DriveType.Fixed)
            throw new IOException($"'{path}' is not on a local fixed drive, so it has no Recycle Bin.");

        var op = new ShFileOpStruct
        {
            wFunc = FO_DELETE,
            pFrom = Path.GetFullPath(path) + "\0\0", // double-null-terminated list
            fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI,
        };
        var result = SHFileOperation(ref op);
        if (result != 0 || op.fAnyOperationsAborted)
            throw new IOException($"Could not move '{path}' to the Recycle Bin (error 0x{result:X}).");
        if (File.Exists(path) || Directory.Exists(path))
            throw new IOException($"'{path}' is still there; the Recycle Bin may be disabled for this drive.");
    }
}
