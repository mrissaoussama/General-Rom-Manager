﻿using SkyEditor.IO.Binary;
using System.Text;
namespace DotNet3dsToolkit.Ctr;

public class NcsdFile : INcchPartitionContainer, IDisposable
{
    private const int MediaUnitSize = 0x200;
    {
        try
        {
            return file.Length >= 0x104 && await file.ReadStringAsync(0x100, 4, Encoding.ASCII) == "NCSD";
        catch (Exception)
        {
            return false;
        }
    }
    {
        var header = new CartridgeNcsdHeader(await data.ReadArrayAsync(0, 0x1500));
        var partitions = new NcchPartition[header.Partitions.Length];
        {
            var partitionStart = (long)header.Partitions[i].Offset * MediaUnitSize;
            var partitionLength = (long)header.Partitions[i].Length * MediaUnitSize;
            partitions[i] = await NcchPartition.Load(data.Slice((ulong)partitionStart, partitionLength));
        }));
    }
    {
        Header = header ?? throw new ArgumentNullException(nameof(partitions));
        {
            throw new ArgumentException("Must provider at least one partition", nameof(partitions));
        }
        {
            throw new ArgumentException("NCSD files cannot have more than 8 partitions", nameof(partitions));
        }
    }
    {
        var exeFsFileSizes = Partitions.Where(P => P.ExeFs != null).Select(p => p.ExeFs.Files.Values.Select(f => f.RawData.Length).Sum()).Sum();
        var totalSize = exeFsFileSizes;
        long cartridgeSize = (long)Math.Pow(2, Math.Ceiling(Math.Log(totalSize * 0x200) / Math.Log(2)));
        var cartridgeSizeInMediaUnits = (cartridgeSize + 0x200 - 1) / 0x200;
        Header.ImageSize = (int)cartridgeSizeInMediaUnits;
    }
    /// Writes the current state of the NCSD partition to the given binary data accessor
    /// </summary>
    /// <param name="data">Data accessor to receive the binary data</param>
    public async Task WriteBinary(IBinaryDataAccessor data)
    {
        long offset = 0x4000;
        var partitionHeaders = new List<NcsdPartitionInfo>();
        for (int i = 0; i < Partitions.Length; i++)
        {
            if (Partitions[i] != null)
            {
                var bytesWritten = await Partitions[i].WriteBinary(data.Slice(offset, data.Length));
                partitionHeaders.Add(new NcsdPartitionInfo
                {
                    CryptType = 0,
                    Length = (int)((bytesWritten + 0x200 - 1) / 0x200),
                    Offset = (int)((offset + 0x200 - 1) / 0x200)
                });
                offset += bytesWritten + (0x200 - (bytesWritten % 0x200));
            }
            else
            {
                partitionHeaders.Add(new NcsdPartitionInfo
                {
                    CryptType = 0,
                    Length = 0,
                    Offset = 0
                });
            }
        }
        await data.WriteAsync(0, headerData);
        await data.WriteAsync(headerData.Length, Enumerable.Repeat<byte>(0xFF, 0x4000 - headerData.Length).ToArray());
    }
    {
        if (Partitions != null)
        {
            foreach (var partition in Partitions)
            {
                partition?.Dispose();
            }
        }
    }
}