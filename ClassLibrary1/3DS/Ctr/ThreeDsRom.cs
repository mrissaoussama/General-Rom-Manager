﻿using DotNet3dsToolkit.Ctr;
using DotNet3dsToolkit.Infrastructure;
using SkyEditor.IO;
using SkyEditor.IO.Binary;
using SkyEditor.IO.FileSystem;
using SkyEditor.IO.FileSystem.Internal;
using SkyEditor.Utilities.AsyncFor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DotNet3dsToolkit
{
    public class ThreeDsRom : IExtendedFileSystem, IDisposable
    {  public  long GetTitleID()
        {
           
            return ((CiaFile)Container).TmdMetadata.TitleId;
        }
        public ushort GetTitleVersion()
        {
            
            return ((CiaFile)Container).TmdMetadata.TitleVersion;
        }
        public long GetSystemVersion()
        {
          
            return ((CiaFile)Container).TmdMetadata.SystemVersion;
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
        public string GetRegion()
        {
            var exefsByteArray = Partitions[0].ExeFs.Files["icon"].RawData;
            int regionLockoutOffset = 0x2018;
            int regionLockoutSize = 0x4;

            // Ensure that the regionLockoutOffset is within the bounds of the exefsByteArray
            if (regionLockoutOffset + regionLockoutSize <= exefsByteArray.Length)
            {
                // Extract the region data from the specified offset and size
                byte[] regionBytes = new byte[regionLockoutSize];
                Array.Copy(exefsByteArray, regionLockoutOffset, regionBytes, 0, regionLockoutSize);

                // Convert the byte array to a uint (assuming little-endian byte order)
                uint regionFlag = BitConverter.ToUInt32(regionBytes, 0);

                // Check each bitmask and return the corresponding region
                Dictionary<uint, string> regionBitmasks = new Dictionary<uint, string>
        {
            { 0x01, "JP" },
            { 0x02, "US" },
            { 0x04, "EUR" },
            { 0x08, "Australia" },
            { 0x10, "China" },
            { 0x20, "Korea" },
            { 0x40, "Taiwan" }
        };

                foreach (var region in regionBitmasks)
                {
                    if ((regionFlag & region.Key) != 0)
                    {
                        return region.Value;
                    }
                }

                // Handle the case where no matching region is found
                return "";
            }
            else
            {
                // Handle the case where the specified offset and size are out of bounds
                return "";
            }
        }

        public string GetLongDescription()
        {
            var exefsByteArray =Partitions[0].ExeFs.Files["icon"].RawData;
            var byteArray = new byte[exefsByteArray.Length];
            Array.Copy(exefsByteArray, byteArray, exefsByteArray.Length);

            int shortDescriptionOffset = 0x8;
            int shortDescriptionSize = 0x80;
            int longDescriptionOffset = shortDescriptionOffset + shortDescriptionSize;
            int longDescriptionSize = 0x80;
            int publisherOffset = longDescriptionOffset + longDescriptionSize;
            int publisherSize = 0x80;
            
            string longDescription = Encoding.Unicode.GetString(byteArray, longDescriptionOffset, longDescriptionSize).TrimEnd('\0');
        
            return longDescription;
        }
        public string GetPublisher()
        {
            var exefsByteArray = Partitions[0].ExeFs.Files["icon"].RawData;
            var byteArray = new byte[exefsByteArray.Length];
            Array.Copy(exefsByteArray, byteArray, exefsByteArray.Length);

            int shortDescriptionOffset = 0x8;
            int shortDescriptionSize = 0x80;
            int longDescriptionOffset = shortDescriptionOffset + shortDescriptionSize;
            int longDescriptionSize = 0x80;
            int publisherOffset = longDescriptionOffset + longDescriptionSize;
            int publisherSize = 0x80;
            string publisher = Encoding.Unicode.GetString(byteArray, publisherOffset, publisherSize).TrimEnd('\0');

            return publisher;
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

        public static async Task<ThreeDsRom> Load(string filename, IFileSystem fileSystem)
        {
            var rom = new ThreeDsRom(fileSystem);
            await rom.OpenFile(filename);
            return rom;
        }

        public static async Task<ThreeDsRom> Load(BinaryFile file)
        {
            var rom = new ThreeDsRom(file);
            await rom.OpenFile(file);
            return rom;
        }

        public static async Task<bool> IsThreeDsRom(BinaryFile file)
        {
            return await NcsdFile.IsNcsd(file)
                || await CiaFile.IsCia(file)
                || await NcchPartition.IsNcch(file)
                || await ExeFs.IsExeFs(file);
        }

        public ThreeDsRom()
        {
            (this as IFileSystem).ResetWorkingDirectory();
            CurrentFileSystem = new PhysicalFileSystem();
        }

        public ThreeDsRom(IFileSystem fileSystem)
        {
            CurrentFileSystem = fileSystem;
        }

        public ThreeDsRom(BinaryFile file)
        {
            RawData = file;
            CurrentFileSystem = new PhysicalFileSystem();
        }


        public ThreeDsRom(ExeFs exefs, int partitionIndex = 0)
        {
            Container = new SingleNcchPartitionContainer(new NcchPartition(exefs: exefs), partitionIndex);
        }

        public ThreeDsRom(NcchPartition ncch, int partitionIndex = 0)
        {
            Container = new SingleNcchPartitionContainer(ncch, partitionIndex);
        }

        private INcchPartitionContainer? Container { get; set; }
        public NcchPartition[] Partitions => Container?.Partitions ?? throw new InvalidOperationException("ROM has not yet been initialized");
        private BinaryFile? RawData { get; set; }
        private IFileSystem? CurrentFileSystem { get; set; }

        public async Task OpenFile(string filename, IFileSystem fileSystem)
        {
            CurrentFileSystem = fileSystem;

            if (fileSystem.FileExists(filename))
            {
                RawData = new BinaryFile(filename, CurrentFileSystem);

                await OpenFile(RawData);
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

        public async Task OpenFile(IReadOnlyBinaryDataAccessor file)
        {
            if (CurrentFileSystem == null)
            {
                throw new InvalidOperationException("ROM has already been initialized");
            }

            // Clear virtual path if it exists
            if (!string.IsNullOrEmpty(VirtualPath) && CurrentFileSystem.DirectoryExists(VirtualPath))
            {
                CurrentFileSystem.DeleteDirectory(VirtualPath);
            }

            VirtualPath = CurrentFileSystem.GetTempDirectory();

            if (await NcsdFile.IsNcsd(file))
            {
                Container = await NcsdFile.Load(file);
            }
            else if (file is BinaryFile binaryFile && await CiaFile.IsCia(binaryFile))
            {
                Container = await CiaFile.Load(file);
            }
            else if (await NcchPartition.IsNcch(file))
            {
                Container = new SingleNcchPartitionContainer(await NcchPartition.Load(file));
            }
         
            else if (await ExeFs.IsExeFs(file))
            {
                Container = new SingleNcchPartitionContainer(new NcchPartition(exefs: await ExeFs.Load(file)));
            }
            else
            {
                throw new BadImageFormatException(Properties.Resources.ThreeDsRom_UnsupportedFileFormat);
            }
        }

        public async Task OpenFile(string filename)
        {
            if (CurrentFileSystem == null)
            {
                throw new InvalidOperationException("ROM has already been initialized");
            }

            await this.OpenFile(filename, CurrentFileSystem);
        }

        public NcchPartition? GetPartitionOrDefault(int partitionIndex)
        {
            if (Partitions.Length > partitionIndex)
            {
                return Partitions[partitionIndex];
            }
            else
            {
                return null;
            }
        }

        public async Task ExtractFiles(string directoryName, IFileSystem fileSystem, ProgressReportToken? progressReportToken = null)
        {
            List<ProcessingProgressedToken>? extractionProgressedTokens = null;
            if (progressReportToken != null)
            {
                extractionProgressedTokens = new List<ProcessingProgressedToken>();
                progressReportToken.IsIndeterminate = false;
            }

            void onExtractionTokenProgressed(object sender, EventArgs e)
            {
                if (progressReportToken != null)
                {
                    progressReportToken.Progress = (float)extractionProgressedTokens.Select(t => t.ProcessedFileCount).Sum() / extractionProgressedTokens.Select(t => t.TotalFileCount).Sum();
                }
            }

            if (!fileSystem.DirectoryExists(directoryName))
            {
                fileSystem.CreateDirectory(directoryName);
            }

            var tasks = new List<Task>();
            for (int i = 0; i < Partitions.Length; i++)
            {
                var partitionIndex = i; // Prevents race conditions with i changing inside Task.Run's
                var partition = GetPartitionOrDefault(i);

                if (partition == null)
                {
                    continue;
                }

                if (partition.ExeFs != null)
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

                if (partition.Header != null)
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

                if (partition.ExHeader != null)
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


            }

            await Task.WhenAll(tasks);

            if (progressReportToken != null)
            {
                progressReportToken.Progress = 1;
                progressReportToken.IsCompleted = true;
            }
        }

        public async Task ExtractFiles(string directoryName, ProgressReportToken? progressReportToken = null)
        {
            await ExtractFiles(directoryName, this.CurrentFileSystem, progressReportToken);
        }

        public string GetRomFsDirectoryName(int partitionId)
        {
            if (Container == null)
            {
                throw new InvalidOperationException("ROM has not yet been initialized");
            }

            if (Container.IsDlcContainer)
            {
                return "RomFS-" + partitionId.ToString();
            }
            else
            {
                return partitionId switch
                {
                    0 => "RomFS",
                    1 => "Manual",
                    2 => "DownloadPlay",
                    6 => "N3DSUpdate",
                    7 => "O3DSUpdate",
                    _ => "RomFS-" + partitionId.ToString(),
                };
            }
        }

        public static string GetExeFsDirectoryName(int partitionId)
        {
            return partitionId switch
            {
                0 => "ExeFS",
                _ => "ExeFS-" + partitionId.ToString(),
            };
        }

        public static string GetHeaderFileName(int partitionId)
        {
            return partitionId switch
            {
                0 => "Header.bin",
                _ => "Header-" + partitionId.ToString() + ".bin",
            };
        }

        public static string GetExHeaderFileName(int partitionId)
        {
            return partitionId switch
            {
                0 => "ExHeader.bin",
                _ => "ExHeader-" + partitionId.ToString() + ".bin",
            };
        }

        public static string GetPlainRegionFileName(int partitionId)
        {
            return partitionId switch
            {
                0 => "PlainRegion.txt",
                _ => "PlainRegion-" + partitionId.ToString() + ".txt",
            };
        }

        public static string GetLogoFileName(int partitionId)
        {
            return partitionId switch
            {
                0 => "Logo.bin",
                _ => "Logo-" + partitionId.ToString() + ".bin",
            };
        }

        public async Task Save(string filename, IFileSystem fileSystem)
        {
            throw new NotImplementedException();
        }

        public async Task Save(string filename)
        {
            await Save(filename, PhysicalFileSystem.Instance);
        }

        #region IDisposable Support
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

                    if (Container is IDisposable disposableContainer)
                    {
                        disposableContainer.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ThreeDsRom() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion


#pragma warning disable CS0162 // Unreachable code detected
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

        /// <summary>
        /// Gets a regular expression for the given search pattern for use with <see cref="GetFiles(string, string, bool)"/>.
        /// </summary>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        private static string GetFileSearchRegex(string searchPattern)
        {
            var asteriskParts = searchPattern.Split('*');
            var regexString = new StringBuilder();

            foreach (var part in asteriskParts)
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

            return regexString.ToString();
        }

        /// <summary>
        /// Keeps track of files that have been logically deleted
        /// </summary>
        private List<string> BlacklistedPaths => new List<string>();

        /// <summary>
        /// Path in the current I/O provider where temporary files are stored
        /// </summary>
        private string? VirtualPath { get; set; }

        /// <summary>
        /// Whether or not to delete <see cref="VirtualPath"/> on delete
        /// </summary>
        private bool DisposeVirtualPath { get; set; }

        string IFileSystem.WorkingDirectory
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

        protected string[] GetPathParts(string path)
        {
            var parts = new List<string>();

            path = path.Replace('\\', '/');
            if (!path.StartsWith("/") && !(_workingDirectoryParts?.Length == 1 && _workingDirectoryParts[0] == string.Empty))
            {
                parts.AddRange(_workingDirectoryParts);
            }

            foreach (var item in path.TrimStart('/').Split('/'))
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
            return parts.ToArray();
        }

        void IFileSystem.ResetWorkingDirectory()
        {
            (this as IFileSystem).WorkingDirectory = "/";
        }

        private string FixPath(string path)
        {
            var fixedPath = path.Replace('\\', '/');

            // Apply working directory
            if (fixedPath.StartsWith("/"))
            {
                return fixedPath;
            }
            else
            {
                return Path.Combine((this as IFileSystem).WorkingDirectory, path);
            }
        }

        private string? GetVirtualPath(string path)
        {
            if (VirtualPath == null)
            {
                return null;
            }
            return Path.Combine(VirtualPath, path.TrimStart('/'));
        }

        private IReadOnlyBinaryDataAccessor? GetDataReference(string[] parts, bool throwIfNotFound = true)
        {
            IReadOnlyBinaryDataAccessor? getExeFsDataReference(string[] pathParts, int partitionId)
            {
                if (pathParts.Length == 2)
                {
                    var data = GetPartitionOrDefault(partitionId)?.ExeFs?.Files[pathParts.Last()]?.RawData;
                    if (data == null)
                    {
                        return null;
                    }
                    return new BinaryFile(data);
                }

                return null;
            }

            IReadOnlyBinaryDataAccessor? dataReference = null;

            var firstDirectory = parts[0].ToLower();
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
              
                case "header.bin":
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

            if (dataReference != null)
            {
                return dataReference;
            }

            if (throwIfNotFound)
            {
                var path = "/" + string.Join("/", parts);
                throw new FileNotFoundException(string.Format(Properties.Resources.ThreeDsRom_ErrorRomFileNotFound, path), path);
            }
            else
            {
                return null;
            }
        }

        long IReadOnlyFileSystem.GetFileLength(string filename)
        {
            var file = GetDataReference(GetPathParts(filename));
            if (file == null)
            {
                throw new FileNotFoundException(string.Format(Properties.Resources.ThreeDsRom_ErrorRomFileNotFound, filename), filename);
            }
            return file.Length;
        }

        bool IReadOnlyFileSystem.FileExists(string filename)
        {
            var virtualPath = GetVirtualPath(filename);
            return (CurrentFileSystem != null && !string.IsNullOrEmpty(virtualPath) && CurrentFileSystem.FileExists(virtualPath))
                || GetDataReference(GetPathParts(filename), false) != null;
        }

        private bool DirectoryExists(string[] parts)
        {
         
            if (parts.Length == 1)
            {
                var dirName = parts[0].ToLower();
                switch (dirName)
                {
                    case "exefs-0":
                    case "exefs":
                        return GetPartitionOrDefault(0)?.ExeFs != null;
                    case "romfs-0":
                   
                    default:
                       
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
                  
                    default:
                      
                        if (dirName.StartsWith("exefs-partition-"))
                        {
                            // Directories inside exefs are not supported
                            return false;
                        }
                        return false;
                }
            }
        }

        bool IReadOnlyFileSystem.DirectoryExists(string path)
        {
            var virtualPath = GetVirtualPath(path);
            return !BlacklistedPaths.Contains(FixPath(path))
                    &&
                    ((CurrentFileSystem != null && !string.IsNullOrEmpty(virtualPath) && CurrentFileSystem.DirectoryExists(virtualPath))
                        || DirectoryExists(GetPathParts(path))
                    );
        }

        void IFileSystem.CreateDirectory(string path)
        {
            var fixedPath = FixPath(path);
            if (BlacklistedPaths.Contains(fixedPath))
            {
                BlacklistedPaths.Remove(fixedPath);
            }

            if (!(this as IFileSystem).DirectoryExists(fixedPath))
            {
                var virtualPath = GetVirtualPath(fixedPath);
                if (!string.IsNullOrEmpty(virtualPath))
                {
                    CurrentFileSystem?.CreateDirectory(virtualPath);
                }
            }
        }

        string[] IReadOnlyFileSystem.GetFiles(string path, string searchPattern, bool topDirectoryOnly)
        {
            var searchPatternRegex = new Regex(GetFileSearchRegex(searchPattern), RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var parts = GetPathParts(path);
            var output = new List<string>();


            var dirName = parts[0].ToLower();
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
                
                default:
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

            // Apply shadowed files
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

            return output.ToArray();
        }

        string[] IReadOnlyFileSystem.GetDirectories(string path, bool topDirectoryOnly)
        {
            var parts = GetPathParts(path);
            var output = new List<string>();

          
            var dirName = parts[0].ToLower();
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
                       
                    }
                    break;
                case "exefs" when parts.Length == 1:
                    // ExeFs doesn't support directories
                    break;
              
            }

            // Apply shadowed files
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

            return output.ToArray();
        }

        byte[] IReadOnlyFileSystem.ReadAllBytes(string filename)
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
                    if (data == null)
                    {
                        throw new FileNotFoundException(string.Format(Properties.Resources.ThreeDsRom_ErrorRomFileNotFound, filename), filename);
                    }
                    return data.ReadArray();
                }
            }
        }

        public IReadOnlyBinaryDataAccessor? GetFileReference(string filename)
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
                    return new BinaryFile(CurrentFileSystem.ReadAllBytes(virtualPath));
                }
                else
                {
                    return GetDataReference(GetPathParts(filename));
                }
            }
        }

        void IExtendedFileSystem.WriteAllBytes(string filename, byte[] data)
        {
            if (CurrentFileSystem == null)
            {
                throw new NotSupportedException("Cannot open a file as a stream without an IO provider.");
            }

            var fixedPath = FixPath(filename);
            if (BlacklistedPaths.Contains(fixedPath))
            {
                BlacklistedPaths.Remove(fixedPath);
            }

            var virtualPath = GetVirtualPath(filename);
            if (!string.IsNullOrEmpty(virtualPath))
            {
                var virtualDir = Path.GetDirectoryName(virtualPath);
                if (!string.IsNullOrEmpty(virtualDir) && !CurrentFileSystem.DirectoryExists(virtualDir))
                {
                    CurrentFileSystem.CreateDirectory(virtualDir);
                }

                CurrentFileSystem.WriteAllBytes(virtualPath, data);
            }
        }

        void IFileSystem.CopyFile(string sourceFilename, string destinationFilename)
        {
            (this as IFileSystem).WriteAllBytes(destinationFilename, (this as IFileSystem).ReadAllBytes(sourceFilename));
        }

        void IFileSystem.DeleteFile(string filename)
        {
            var fixedPath = FixPath(filename);
            if (!BlacklistedPaths.Contains(fixedPath))
            {
                BlacklistedPaths.Add(fixedPath);
            }

            var virtualPath = GetVirtualPath(filename);
            if (!string.IsNullOrEmpty(virtualPath) && CurrentFileSystem != null && CurrentFileSystem.FileExists(virtualPath))
            {
                CurrentFileSystem.DeleteFile(virtualPath);
            }
        }

        void IFileSystem.DeleteDirectory(string path)
        {
            var fixedPath = FixPath(path);
            if (!BlacklistedPaths.Contains(fixedPath))
            {
                BlacklistedPaths.Add(fixedPath);
            }

            var virtualPath = GetVirtualPath(path);
            if (CurrentFileSystem != null && !string.IsNullOrEmpty(virtualPath) && CurrentFileSystem.FileExists(virtualPath))
            {
                CurrentFileSystem.DeleteFile(virtualPath);
            }
        }

        string IFileSystem.GetTempFilename()
        {
            // The class can't map temp files to the underlying file system yet
            throw new NotImplementedException();

            var path = "/temp/files/" + Guid.NewGuid().ToString();
            (this as IFileSystem).WriteAllBytes(path, new byte[] { });
            return path;
        }

        string IFileSystem.GetTempDirectory()
        {
            // The class can't map temp files to the underlying file system yet
            throw new NotImplementedException();

            var path = "/temp/dirs/" + Guid.NewGuid().ToString();
            (this as IFileSystem).CreateDirectory(path);
            return path;
        }

        Stream IFileSystem.OpenFile(string filename)
        {
            if (CurrentFileSystem == null)
            {
                throw new NotSupportedException("Cannot open a file as a stream without an IO provider.");
            }

            var virtualPath = GetVirtualPath(filename);
            if (!string.IsNullOrEmpty(virtualPath))
            {
                var virtualDir = Path.GetDirectoryName(virtualPath);
                if (!string.IsNullOrEmpty(virtualDir) && !CurrentFileSystem.DirectoryExists(virtualDir))
                {
                    CurrentFileSystem.CreateDirectory(virtualDir);
                }
                CurrentFileSystem.WriteAllBytes(virtualPath, (this as IFileSystem).ReadAllBytes(filename));
            }

            return CurrentFileSystem.OpenFile(virtualPath!);
        }

        Stream IReadOnlyFileSystem.OpenFileReadOnly(string filename)
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

            var virtualPath = GetVirtualPath(filename);
            if (!string.IsNullOrEmpty(virtualPath))
            {
                var virtualDir = Path.GetDirectoryName(virtualPath);
                if (!string.IsNullOrEmpty(virtualDir) && !CurrentFileSystem.DirectoryExists(virtualDir))
                {
                    CurrentFileSystem.CreateDirectory(virtualDir);
                }
                CurrentFileSystem.WriteAllBytes(virtualPath, (this as IFileSystem).ReadAllBytes(filename));
            }

            return CurrentFileSystem.OpenFileReadOnly(virtualPath!);
        }

        Stream IFileSystem.OpenFileWriteOnly(string filename)
        {
            if (CurrentFileSystem == null)
            {
                throw new NotSupportedException("Cannot open a file as a stream without an IO provider.");
            }

            var virtualPath = GetVirtualPath(filename);
            if (!string.IsNullOrEmpty(virtualPath))
            {
                var virtualDir = Path.GetDirectoryName(virtualPath);
                if (!string.IsNullOrEmpty(virtualDir) && !CurrentFileSystem.DirectoryExists(virtualDir))
                {
                    CurrentFileSystem.CreateDirectory(virtualDir);
                }
                CurrentFileSystem.WriteAllBytes(virtualPath, (this as IFileSystem).ReadAllBytes(filename));
            }

            return CurrentFileSystem.OpenFileWriteOnly(virtualPath!);
        }

        void IExtendedFileSystem.WriteAllText(string filename, string data, Encoding encoding)
        {
            (this as IExtendedFileSystem).WriteAllBytes(filename, encoding.GetBytes(data));
        }

        Task IExtendedFileSystem.WriteAllTextAsync(string filename, string data, Encoding encoding)
        {
            (this as IExtendedFileSystem).WriteAllText(filename, data, encoding);
            return Task.CompletedTask;
        }

        Task IExtendedFileSystem.WriteAllBytesAsync(string filename, byte[] data)
        {
            (this as IExtendedFileSystem).WriteAllBytes(filename, data);
            return Task.CompletedTask;
        }

        #endregion
#pragma warning restore CS0162 // Unreachable code detected
    }
}
