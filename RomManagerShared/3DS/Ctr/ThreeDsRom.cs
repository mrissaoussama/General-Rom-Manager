﻿using DotNet3dsToolkit.Ctr;
using DotNet3dsToolkit.Infrastructure;
using RomManagerShared.Base;
using SkyEditor.IO.Binary;
using SkyEditor.IO.FileSystem;
using SkyEditor.IO.FileSystem.Internal;
using SkyEditor.Utilities.AsyncFor;
using System.Text;
using System.Text.RegularExpressions;
namespace DotNet3dsToolkit;

public class ThreeDsRom : IExtendedFileSystem, IDisposable
{
    public long GetTitleID()
    {
    }
    public ushort GetTitleVersion()
    {
    }
    public long GetSystemVersion()
    {
    }
    public int GetTitleType()
    {
        return ((CiaFile)Container).TmdMetadata.TitleType;
    }
    public string? GetProductCode()
    {
        return Partitions[0]?.Header?.ProductCode;
    }
    //titlename
    public string GetShortDescription()
    {
        var exefsByteArray = Partitions[0].ExeFs.Files["icon"].RawData;
        var byteArray = new byte[exefsByteArray.Length];
        Array.Copy(exefsByteArray, byteArray, exefsByteArray.Length);
        int shortDescriptionOffset = 0x8;
        int shortDescriptionSize = 0x80;
        string shortDescription = Encoding.Unicode.GetString(byteArray, shortDescriptionOffset, shortDescriptionSize).TrimEnd('\0');
        return shortDescription;
    }
    public Region GetRegion()
    {
        var exefsByteArray = Partitions[0].ExeFs.Files["icon"].RawData;
        int regionLockoutOffset = 0x2018;
        int regionLockoutSize = 0x4;
        if (regionLockoutOffset + regionLockoutSize <= exefsByteArray.Length)
        {
            // Extract the region data from the specified offset and size
            byte[] regionBytes = new byte[regionLockoutSize];
            Array.Copy(exefsByteArray, regionLockoutOffset, regionBytes, 0, regionLockoutSize);
            uint regionFlag = BitConverter.ToUInt32(regionBytes, 0);
            Dictionary<uint, Region> regionBitmasks = new()
            {
        { 0x01, Region.Japan },
        { 0x02, Region.USA },
        { 0x04, Region.Europe },
        { 0x08, Region.Australia },
        { 0x10, Region.China},
        { 0x20, Region.Korea },
        { 0x40, Region.Taiwan }
    };
            {
                if ((regionFlag & region.Key) != 0)
                {
                    return region.Value;
                }
            }
            return Region.Unknown;
        }
        else
        {
            // Handle the case where the specified offset and size are out of bounds
            return Region.Unknown;
        }
    }
    {
        var exefsByteArray = Partitions[0].ExeFs.Files["icon"].RawData;
        var byteArray = new byte[exefsByteArray.Length];
        Array.Copy(exefsByteArray, byteArray, exefsByteArray.Length);
        int shortDescriptionSize = 0x80;
        int longDescriptionOffset = shortDescriptionOffset + shortDescriptionSize;
        int longDescriptionSize = 0x80;
        int publisherOffset = longDescriptionOffset + longDescriptionSize;
        int publisherSize = 0x80;
    }
    public string GetPublisher()
    {
        var exefsByteArray = Partitions[0].ExeFs.Files["icon"].RawData;
        var byteArray = new byte[exefsByteArray.Length];
        Array.Copy(exefsByteArray, byteArray, exefsByteArray.Length);
        int shortDescriptionSize = 0x80;
        int longDescriptionOffset = shortDescriptionOffset + shortDescriptionSize;
        int longDescriptionSize = 0x80;
        int publisherOffset = longDescriptionOffset + longDescriptionSize;
        int publisherSize = 0x80;
        string publisher = Encoding.Unicode.GetString(byteArray, publisherOffset, publisherSize).TrimEnd('\0');
    }
    public string GetVersion()
    {
        var exefsByteArray = Partitions[0].ExeFs.Files["icon"].RawData;
        var byteArray = new byte[exefsByteArray.Length];
        Array.Copy(exefsByteArray, byteArray, exefsByteArray.Length);
        var versionOffset = 0x04;
        var versionSize = 0x02;
        var versionBytes = new byte[versionSize];
        Array.Copy(byteArray, versionOffset, versionBytes, 0, versionSize);
        Array.Reverse(versionBytes);
        string version = Encoding.Unicode.GetString(versionBytes).TrimEnd('\0');
        return version;
    }
    public static async Task<ThreeDsRom> Load(string filename)
    {
        var rom = new ThreeDsRom();
        await rom.OpenFile(filename);
        return rom;
    }
    {
        var rom = new ThreeDsRom(fileSystem);
        await rom.OpenFile(filename);
        return rom;
    }
    {
        var rom = new ThreeDsRom(file);
        await rom.OpenFile(file);
        return rom;
    }
    {
        return await NcsdFile.IsNcsd(file)
            || await CiaFile.IsCia(file)
            || await NcchPartition.IsNcch(file)
            || await ExeFs.IsExeFs(file);
    }
    {
        (this as IFileSystem).ResetWorkingDirectory();
        CurrentFileSystem = new PhysicalFileSystem();
    }
    {
        CurrentFileSystem = fileSystem;
    }
    {
        RawData = file;
        CurrentFileSystem = new PhysicalFileSystem();
    }
    {
        Container = new SingleNcchPartitionContainer(new NcchPartition(exefs: exefs), partitionIndex);
    }
    {
        Container = new SingleNcchPartitionContainer(ncch, partitionIndex);
    }
    public NcchPartition[] Partitions => Container?.Partitions ?? throw new InvalidOperationException("ROM has not yet been initialized");
    private BinaryFile? RawData { get; set; }
    private IFileSystem? CurrentFileSystem { get; set; }
    {
        CurrentFileSystem = fileSystem;
        {
            RawData = new BinaryFile(filename, CurrentFileSystem);
        }
        else if (fileSystem.DirectoryExists(filename))
        {
            VirtualPath = filename;
            DisposeVirtualPath = false;
        }
        else
        {
            throw new FileNotFoundException("Could not find file or directory at the given path", filename);
        }
    }
    {
        if (CurrentFileSystem == null)
        {
            throw new InvalidOperationException("ROM has already been initialized");
        }
        if (!string.IsNullOrEmpty(VirtualPath) && CurrentFileSystem.DirectoryExists(VirtualPath))
        {
            CurrentFileSystem.DeleteDirectory(VirtualPath);
        }
        {
            Container = await NcsdFile.Load(file);
        }
        else if (file is BinaryFile binaryFile && await CiaFile.IsCia(binaryFile))
        {
            Container = await CiaFile.Load(file);
        }
        else
        {
            Container = await NcchPartition.IsNcch(file)
                ? new SingleNcchPartitionContainer(await NcchPartition.Load(file))
                : await ExeFs.IsExeFs(file)
                            ? (INcchPartitionContainer)new SingleNcchPartitionContainer(new NcchPartition(exefs: await ExeFs.Load(file)))
                            : throw new BadImageFormatException(Properties.Resources.ThreeDsRom_UnsupportedFileFormat);
        }
    {
        if (CurrentFileSystem == null)
        {
            throw new InvalidOperationException("ROM has already been initialized");
        }
    }
    {
        return Partitions.Length > partitionIndex ? Partitions[partitionIndex] : null;
    }
    {
        List<ProcessingProgressedToken>? extractionProgressedTokens = null;
        if (progressReportToken != null)
        {
            extractionProgressedTokens = [];
            progressReportToken.IsIndeterminate = false;
        }
        {
            if (progressReportToken != null)
            {
                progressReportToken.Progress = (float)extractionProgressedTokens.Select(t => t.ProcessedFileCount).Sum() / extractionProgressedTokens.Select(t => t.TotalFileCount).Sum();
            }
        }
        {
            fileSystem.CreateDirectory(directoryName);
        }
        for (int i = 0; i < Partitions.Length; i++)
        {
            var partitionIndex = i; // Prevents race conditions with i changing inside Task.Run's
            var partition = GetPartitionOrDefault(i);
            {
                continue;
            }
            {
                ProcessingProgressedToken? exefsExtractionProgressedToken = null;
                if (exefsExtractionProgressedToken != null && extractionProgressedTokens != null)
                {
                    exefsExtractionProgressedToken = new ProcessingProgressedToken();
                    exefsExtractionProgressedToken.FileCountChanged += onExtractionTokenProgressed;
                    extractionProgressedTokens.Add(exefsExtractionProgressedToken);
                }
                tasks.Add(partition.ExeFs.ExtractFiles(Path.Combine(directoryName, GetExeFsDirectoryName(partitionIndex)), fileSystem, exefsExtractionProgressedToken));
            }
            {
                ProcessingProgressedToken? exefsExtractionProgressedToken = null;
                if (exefsExtractionProgressedToken != null && extractionProgressedTokens != null)
                {
                    exefsExtractionProgressedToken = new ProcessingProgressedToken
                    {
                        TotalFileCount = 1
                    };
                    exefsExtractionProgressedToken.FileCountChanged += onExtractionTokenProgressed;
                    extractionProgressedTokens.Add(exefsExtractionProgressedToken);
                }
                tasks.Add(Task.Run(() =>
                {
                    fileSystem.WriteAllBytes(Path.Combine(directoryName, GetHeaderFileName(partitionIndex)), partition.Header.ToBinary().ReadArray());
                    exefsExtractionProgressedToken?.IncrementProcessedFileCount();
                }));
            }
            {
                ProcessingProgressedToken? exefsExtractionProgressedToken = null;
                if (exefsExtractionProgressedToken != null && extractionProgressedTokens != null)
                {
                    exefsExtractionProgressedToken = new ProcessingProgressedToken
                    {
                        TotalFileCount = 1
                    };
                    exefsExtractionProgressedToken.FileCountChanged += onExtractionTokenProgressed;
                    extractionProgressedTokens.Add(exefsExtractionProgressedToken);
                }
                tasks.Add(Task.Run(() =>
                {
                    fileSystem.WriteAllBytes(Path.Combine(directoryName, GetExHeaderFileName(partitionIndex)), partition.ExHeader.ToByteArray());
                    exefsExtractionProgressedToken?.IncrementProcessedFileCount();
                }));
            }
        {
            progressReportToken.Progress = 1;
            progressReportToken.IsCompleted = true;
        }
    }
    {
        await ExtractFiles(directoryName, this.CurrentFileSystem, progressReportToken);
    }
    {
        return Container == null
            ? throw new InvalidOperationException("ROM has not yet been initialized")
            : Container.IsDlcContainer
            ? "RomFS-" + partitionId.ToString()
            : partitionId switch
            {
                0 => "RomFS",
                1 => "Manual",
                2 => "DownloadPlay",
                6 => "N3DSUpdate",
                7 => "O3DSUpdate",
                _ => "RomFS-" + partitionId.ToString(),
            };
    {
        return partitionId switch
        {
            0 => "ExeFS",
            _ => "ExeFS-" + partitionId.ToString(),
        };
    }
    {
        return partitionId switch
        {
            0 => "Header.bin",
            _ => "Header-" + partitionId.ToString() + ".bin",
        };
    }
    {
        return partitionId switch
        {
            0 => "ExHeader.bin",
            _ => "ExHeader-" + partitionId.ToString() + ".bin",
        };
    }
    {
        return partitionId switch
        {
            0 => "PlainRegion.txt",
            _ => "PlainRegion-" + partitionId.ToString() + ".txt",
        };
    }
    {
        return partitionId switch
        {
            0 => "Logo.bin",
            _ => "Logo-" + partitionId.ToString() + ".bin",
        };
    }
    {
        throw new NotImplementedException();
    }
    {
        await Save(filename, PhysicalFileSystem.Instance);
    }
    private bool disposedValue = false; // To detect redundant calls
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                RawData?.Dispose();
                if (CurrentFileSystem != null && !string.IsNullOrEmpty(VirtualPath) && CurrentFileSystem.DirectoryExists(VirtualPath))
                {
                    CurrentFileSystem.DeleteDirectory(VirtualPath);
                }
                {
                    disposableContainer.Dispose();
                }
            }
            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.
            disposedValue = true;
        }
    }
    // ~ThreeDsRom() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        // GC.SuppressFinalize(this);
    }
    #region IIOProvider Implementation
    /// <summary>
    /// Gets a regular expression for the given search pattern for use with <see cref="GetFiles(string, string, bool)"/>.  Do not provide asterisks.
    /// </summary>
    private static StringBuilder GetFileSearchRegexQuestionMarkOnly(string searchPattern)
    {
        var parts = searchPattern.Split('?');
        var regexString = new StringBuilder();
        foreach (var item in parts)
        {
            regexString.Append(Regex.Escape(item));
            if (item != parts.Last())
            {
                regexString.Append(".?");
            }
        }
        return regexString;
    }
    /// Gets a regular expression for the given search pattern for use with <see cref="GetFiles(string, string, bool)"/>.
    /// </summary>
    /// <param name="searchPattern"></param>
    /// <returns></returns>
    private static string GetFileSearchRegex(string searchPattern)
    {
        var asteriskParts = searchPattern.Split('*');
        var regexString = new StringBuilder();
        {
            if (string.IsNullOrEmpty(part))
            {
                // Asterisk
                regexString.Append(".*");
            }
            else
            {
                regexString.Append(GetFileSearchRegexQuestionMarkOnly(part));
            }
        }
    }
    /// Keeps track of files that have been logically deleted
    /// </summary>
    private static List<string> BlacklistedPaths => new();
    /// Path in the current I/O provider where temporary files are stored
    /// </summary>
    private string? VirtualPath { get; set; }
    /// Whether or not to delete <see cref="VirtualPath"/> on delete
    /// </summary>
    private bool DisposeVirtualPath { get; set; }
    {
        get
        {
            var path = new StringBuilder();
            foreach (var item in _workingDirectoryParts ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrEmpty(item))
                {
                    path.Append("/");
                    path.Append(item);
                }
            }
            path.Append("/");
            return path.ToString();
        }
        set
        {
            _workingDirectoryParts = GetPathParts(value);
        }
    }
    private string[]? _workingDirectoryParts;
    {
        var parts = new List<string>();
        if (!path.StartsWith("/") && !(_workingDirectoryParts?.Length == 1 && _workingDirectoryParts[0] == string.Empty))
        {
            parts.AddRange(_workingDirectoryParts);
        }
        {
            switch (item)
            {
                case "":
                case ".":
                    break;
                case "..":
                    parts.RemoveAt(parts.Count - 1);
                    break;
                default:
                    parts.Add(item);
                    break;
            }
        }
        if (parts.Count == 0)
        {
            parts.Add(string.Empty);
        }
        return [.. parts];
    }
    {
        (this as IFileSystem).WorkingDirectory = "/";
    }
    {
        var fixedPath = path.Replace('\\', '/');
        return fixedPath.StartsWith("/") ? fixedPath : Path.Combine((this as IFileSystem).WorkingDirectory, path);
    }
    {
        return VirtualPath == null ? null : Path.Combine(VirtualPath, path.TrimStart('/'));
    }
    {
        IReadOnlyBinaryDataAccessor? getExeFsDataReference(string[] pathParts, int partitionId)
        {
            if (pathParts.Length == 2)
            {
                var data = GetPartitionOrDefault(partitionId)?.ExeFs?.Files[pathParts.Last()]?.RawData;
                return data == null ? null : (IReadOnlyBinaryDataAccessor)new BinaryFile(data);
            }
        }
        switch (firstDirectory)
        {
            case "ncsdheader.bin":
                if (Container is NcsdFile ncsd)
                {
                    dataReference = new BinaryFile(ncsd.Header.ToByteArray());
                }
                break;
            case "exefs-0":
            case "exefs":
                dataReference = getExeFsDataReference(parts, 0);
                break;
                dataReference = GetPartitionOrDefault(0)?.Header?.ToBinary();
                break;
            case "exheader.bin":
                dataReference = GetPartitionOrDefault(0)?.ExHeader != null ? new BinaryFile(GetPartitionOrDefault(0)!.ExHeader!.ToByteArray()) : null;
                break;
            case "plainregion.txt":
                dataReference = GetPartitionOrDefault(0)?.PlainRegion != null ? new BinaryFile(Encoding.ASCII.GetBytes(GetPartitionOrDefault(0)!.PlainRegion)) : null;
                break;
            case "logo.bin":
                dataReference = GetPartitionOrDefault(0)?.Logo != null ? new BinaryFile(GetPartitionOrDefault(0)!.Logo) : null;
                break;
            default:
                if (firstDirectory.StartsWith("exefs-"))
                {
                    var partitionNumRaw = firstDirectory.Split("-".ToCharArray(), 2)[1];
                    if (int.TryParse(partitionNumRaw, out var partitionNum))
                    {
                        dataReference = getExeFsDataReference(parts, partitionNum);
                    }
                }
                else if (firstDirectory.StartsWith("header-"))
                {
                    var partitionNumRaw = firstDirectory.Split("-".ToCharArray(), 2)[1].Split(".".ToCharArray(), 2)[0];
                    if (int.TryParse(partitionNumRaw, out var partitionNum))
                    {
                        dataReference = GetPartitionOrDefault(partitionNum)?.Header?.ToBinary();
                    }
                }
                else if (firstDirectory.StartsWith("exheader-"))
                {
                    var partitionNumRaw = firstDirectory.Split("-".ToCharArray(), 2)[1].Split(".".ToCharArray(), 2)[0];
                    if (int.TryParse(partitionNumRaw, out var partitionNum))
                    {
                        dataReference = GetPartitionOrDefault(partitionNum)?.ExHeader != null ? new BinaryFile(GetPartitionOrDefault(partitionNum)!.ExHeader!.ToByteArray()) : null;
                    }
                }
                else if (firstDirectory.StartsWith("plainregion-"))
                {
                    var partitionNumRaw = firstDirectory.Split("-".ToCharArray(), 2)[1].Split(".".ToCharArray(), 2)[0];
                    if (int.TryParse(partitionNumRaw, out var partitionNum))
                    {
                        dataReference = GetPartitionOrDefault(partitionNum)?.PlainRegion != null ? new BinaryFile(Encoding.ASCII.GetBytes(GetPartitionOrDefault(partitionNum)?.PlainRegion)) : null;
                    }
                }
                else if (firstDirectory.StartsWith("logo-"))
                {
                    var partitionNumRaw = firstDirectory.Split("-".ToCharArray(), 2)[1].Split(".".ToCharArray(), 2)[0];
                    if (int.TryParse(partitionNumRaw, out var partitionNum))
                    {
                        dataReference = GetPartitionOrDefault(partitionNum)?.Logo != null ? new BinaryFile(GetPartitionOrDefault(partitionNum)!.Logo) : null;
                    }
                }
                break;
        }
        {
            return dataReference;
        }
        {
            var path = "/" + string.Join("/", parts);
            throw new FileNotFoundException(string.Format(Properties.Resources.ThreeDsRom_ErrorRomFileNotFound, path), path);
        }
        else
        {
            return null;
        }
    }
    {
        var file = GetDataReference(GetPathParts(filename));
        return file == null
            ? throw new FileNotFoundException(string.Format(Properties.Resources.ThreeDsRom_ErrorRomFileNotFound, filename), filename)
            : file.Length;
    }
    {
        var virtualPath = GetVirtualPath(filename);
        return (CurrentFileSystem != null && !string.IsNullOrEmpty(virtualPath) && CurrentFileSystem.FileExists(virtualPath))
            || GetDataReference(GetPathParts(filename), false) != null;
    }
    {
        {
            var dirName = parts[0].ToLower();
            switch (dirName)
            {
                case "exefs-0":
                case "exefs":
                    return GetPartitionOrDefault(0)?.ExeFs != null;
                case "romfs-0":
                    if (dirName.StartsWith("exefs-"))
                    {
                        var partitionNumRaw = dirName.Split("-".ToCharArray(), 2)[1];
                        if (int.TryParse(partitionNumRaw, out var partitionNum))
                        {
                            return GetPartitionOrDefault(partitionNum)?.ExeFs != null;
                        }
                    }
                    return false;
            }
        }
        else if (parts.Length == 0)
        {
            throw new ArgumentException("Argument cannot be empty", nameof(parts));
        }
        else
        {
            var dirName = parts[0].ToLower();
            switch (dirName)
            {
                case "exefs-0":
                case "exefs":
                    // Directories inside exefs are not supported
                    return false;
                    {
                        // Directories inside exefs are not supported
                        return false;
                    }
                    return false;
            }
        }
    }
    {
        var virtualPath = GetVirtualPath(path);
        return !BlacklistedPaths.Contains(FixPath(path))
                &&
                ((CurrentFileSystem != null && !string.IsNullOrEmpty(virtualPath) && CurrentFileSystem.DirectoryExists(virtualPath))
                    || DirectoryExists(GetPathParts(path))
                );
    }
    {
        var fixedPath = FixPath(path);
        if (BlacklistedPaths.Contains(fixedPath))
        {
            BlacklistedPaths.Remove(fixedPath);
        }
        {
            var virtualPath = GetVirtualPath(fixedPath);
            if (!string.IsNullOrEmpty(virtualPath))
            {
                CurrentFileSystem?.CreateDirectory(virtualPath);
            }
        }
    }
    {
        var searchPatternRegex = new Regex(GetFileSearchRegex(searchPattern), RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var parts = GetPathParts(path);
        var output = new List<string>();
        switch (dirName)
        {
            case "" when parts.Length == 1:
                if (!topDirectoryOnly)
                {
                    for (int i = 0; i < Partitions.Length; i++)
                    {
                        if (Container is NcsdFile ncsd)
                        {
                            output.Add("NcsdHeader.bin");
                        }
                        if (Partitions[i].Header != null)
                        {
                            var headerName = GetHeaderFileName(i);
                            if (!searchPatternRegex.IsMatch(headerName))
                            {
                                output.Add(headerName);
                            }
                        }
                        if (Partitions[i].ExHeader != null)
                        {
                            var exheaderName = GetExHeaderFileName(i);
                            if (!searchPatternRegex.IsMatch(exheaderName))
                            {
                                output.Add(exheaderName);
                            }
                        }
                        if (Partitions[i].PlainRegion != null)
                        {
                            var plainRegionName = GetPlainRegionFileName(i);
                            if (!searchPatternRegex.IsMatch(plainRegionName))
                            {
                                output.Add(plainRegionName);
                            }
                        }
                        if (Partitions[i].Logo != null)
                        {
                            var logoName = GetLogoFileName(i);
                            if (!searchPatternRegex.IsMatch(logoName))
                            {
                                output.Add(logoName);
                            }
                        }
                        if (Partitions[i].ExeFs != null)
                        {
                            output.AddRange((this as IFileSystem).GetFiles("/" + GetExeFsDirectoryName(i), searchPattern, topDirectoryOnly));
                        }
                }
                break;
            case "exefs" when parts.Length == 1:
            case "exefs-0" when parts.Length == 1:
                foreach (var file in GetPartitionOrDefault(0)?.ExeFs?.Files.Keys
                    ?.Where(f => searchPatternRegex.IsMatch(f) && !string.IsNullOrWhiteSpace(f)))
                {
                    output.Add("/ExeFS/" + file);
                }
                break;
                if (dirName.StartsWith("exefs-"))
                {
                    var partitionNumRaw = dirName.Split("-".ToCharArray(), 2)[1];
                    if (int.TryParse(partitionNumRaw, out var partitionNum))
                    {
                        foreach (var file in GetPartitionOrDefault(partitionNum)?.ExeFs?.Files.Keys
                         ?.Where(f => searchPatternRegex.IsMatch(f) && !string.IsNullOrWhiteSpace(f)))
                        {
                            output.Add(GetExeFsDirectoryName(partitionNum) + "/" + file);
                        }
                    }
                }
                break;
        }
        var virtualPath = GetVirtualPath(path);
        if (CurrentFileSystem != null && !string.IsNullOrEmpty(virtualPath) && CurrentFileSystem.DirectoryExists(virtualPath))
        {
            foreach (var item in CurrentFileSystem.GetFiles(virtualPath, searchPattern, topDirectoryOnly))
            {
                var overlayPath = "/" + PathUtilities.MakeRelativePath(item, VirtualPath);
                if (!BlacklistedPaths.Contains(overlayPath) && !output.Contains(overlayPath, StringComparer.OrdinalIgnoreCase))
                {
                    output.Add(overlayPath);
                }
            }
        }
    }
    {
        var parts = GetPathParts(path);
        var output = new List<string>();
        switch (dirName)
        {
            case "" when parts.Length == 1:
                for (int i = 0; i < Partitions.Length; i++)
                {
                    if (Partitions[i].ExeFs != null)
                    {
                        output.Add("/" + GetExeFsDirectoryName(i) + "/");
                        if (!topDirectoryOnly)
                        {
                            output.AddRange((this as IFileSystem).GetDirectories("/" + GetExeFsDirectoryName(i), topDirectoryOnly));
                        }
                    }
                break;
            case "exefs" when parts.Length == 1:
                // ExeFs doesn't support directories
                break;
        var virtualPath = GetVirtualPath(path);
        if (CurrentFileSystem != null && !string.IsNullOrEmpty(virtualPath) && CurrentFileSystem.DirectoryExists(virtualPath))
        {
            foreach (var item in CurrentFileSystem.GetDirectories(virtualPath, topDirectoryOnly))
            {
                var overlayPath = "/" + PathUtilities.MakeRelativePath(item, VirtualPath);
                if (!BlacklistedPaths.Contains(overlayPath) && !output.Contains(overlayPath, StringComparer.OrdinalIgnoreCase))
                {
                    output.Add(overlayPath);
                }
            }
        }
    }
    {
        var fixedPath = FixPath(filename);
        if (BlacklistedPaths.Contains(fixedPath))
        {
            throw new FileNotFoundException(string.Format(Properties.Resources.ThreeDsRom_ErrorRomFileNotFound, filename), filename);
        }
        else
        {
            var virtualPath = GetVirtualPath(fixedPath);
            if (CurrentFileSystem != null && !string.IsNullOrEmpty(virtualPath) && CurrentFileSystem.FileExists(virtualPath))
            {
                return CurrentFileSystem.ReadAllBytes(virtualPath);
            }
            else
            {
                var data = GetDataReference(GetPathParts(filename));
                return data == null
                    ? throw new FileNotFoundException(string.Format(Properties.Resources.ThreeDsRom_ErrorRomFileNotFound, filename), filename)
                    : data.ReadArray();
            }
        }
    }
    {
        var fixedPath = FixPath(filename);
        if (BlacklistedPaths.Contains(fixedPath))
        {
            throw new FileNotFoundException(string.Format(Properties.Resources.ThreeDsRom_ErrorRomFileNotFound, filename), filename);
        }
        else
        {
            var virtualPath = GetVirtualPath(fixedPath);
            return CurrentFileSystem != null && !string.IsNullOrEmpty(virtualPath) && CurrentFileSystem.FileExists(virtualPath)
                ? new BinaryFile(CurrentFileSystem.ReadAllBytes(virtualPath))
                : GetDataReference(GetPathParts(filename));
        }
    }
    {
        if (CurrentFileSystem == null)
        {
            throw new NotSupportedException("Cannot open a file as a stream without an IO provider.");
        }
        if (BlacklistedPaths.Contains(fixedPath))
        {
            BlacklistedPaths.Remove(fixedPath);
        }
        if (!string.IsNullOrEmpty(virtualPath))
        {
            var virtualDir = Path.GetDirectoryName(virtualPath);
            if (!string.IsNullOrEmpty(virtualDir) && !CurrentFileSystem.DirectoryExists(virtualDir))
            {
                CurrentFileSystem.CreateDirectory(virtualDir);
            }
        }
    }
    {
        this.WriteAllBytes(destinationFilename, (this as IFileSystem).ReadAllBytes(sourceFilename));
    }
    {
        var fixedPath = FixPath(filename);
        if (!BlacklistedPaths.Contains(fixedPath))
        {
            BlacklistedPaths.Add(fixedPath);
        }
        if (!string.IsNullOrEmpty(virtualPath) && CurrentFileSystem != null && CurrentFileSystem.FileExists(virtualPath))
        {
            CurrentFileSystem.DeleteFile(virtualPath);
        }
    }
    {
        var fixedPath = FixPath(path);
        if (!BlacklistedPaths.Contains(fixedPath))
        {
            BlacklistedPaths.Add(fixedPath);
        }
        if (CurrentFileSystem != null && !string.IsNullOrEmpty(virtualPath) && CurrentFileSystem.FileExists(virtualPath))
        {
            CurrentFileSystem.DeleteFile(virtualPath);
        }
    }
    {
        // The class can't map temp files to the underlying file system yet
        throw new NotImplementedException();
        this.WriteAllBytes(path, Array.Empty<byte>());
        return path;
    }
    {
        // The class can't map temp files to the underlying file system yet
        throw new NotImplementedException();
        (this as IFileSystem).CreateDirectory(path);
        return path;
    }
    {
        if (CurrentFileSystem == null)
        {
            throw new NotSupportedException("Cannot open a file as a stream without an IO provider.");
        }
        if (!string.IsNullOrEmpty(virtualPath))
        {
            var virtualDir = Path.GetDirectoryName(virtualPath);
            if (!string.IsNullOrEmpty(virtualDir) && !CurrentFileSystem.DirectoryExists(virtualDir))
            {
                CurrentFileSystem.CreateDirectory(virtualDir);
            }
            CurrentFileSystem.WriteAllBytes(virtualPath, (this as IFileSystem).ReadAllBytes(filename));
        }
    }
    {
        if (CurrentFileSystem == null)
        {
            if ((this as IFileSystem).FileExists(filename))
            {
                var data = (this as IFileSystem).ReadAllBytes(filename);
                return new MemoryStream(data);
            }
            else
            {
                throw new NotSupportedException("Cannot open a file as a stream without an IO provider.");
            }
        }
        if (!string.IsNullOrEmpty(virtualPath))
        {
            var virtualDir = Path.GetDirectoryName(virtualPath);
            if (!string.IsNullOrEmpty(virtualDir) && !CurrentFileSystem.DirectoryExists(virtualDir))
            {
                CurrentFileSystem.CreateDirectory(virtualDir);
            }
            CurrentFileSystem.WriteAllBytes(virtualPath, (this as IFileSystem).ReadAllBytes(filename));
        }
    }
    {
        if (CurrentFileSystem == null)
        {
            throw new NotSupportedException("Cannot open a file as a stream without an IO provider.");
        }
        if (!string.IsNullOrEmpty(virtualPath))
        {
            var virtualDir = Path.GetDirectoryName(virtualPath);
            if (!string.IsNullOrEmpty(virtualDir) && !CurrentFileSystem.DirectoryExists(virtualDir))
            {
                CurrentFileSystem.CreateDirectory(virtualDir);
            }
            CurrentFileSystem.WriteAllBytes(virtualPath, (this as IFileSystem).ReadAllBytes(filename));
        }
    }
    {
        (this as IExtendedFileSystem).WriteAllBytes(filename, encoding.GetBytes(data));
    }
    {
        (this as IExtendedFileSystem).WriteAllText(filename, data, encoding);
        return Task.CompletedTask;
    }
    {
        (this as IExtendedFileSystem).WriteAllBytes(filename, data);
        return Task.CompletedTask;
    }
#pragma warning restore CS0162 // Unreachable code detected
}