﻿using SkyEditor.IO.Binary;
namespace DotNet3dsToolkit.Ctr;

public class CiaFile : INcchPartitionContainer, IDisposable
{
    public static Task<bool> IsCia(BinaryFile file)
    {
        // To-do: look at the actual data
        return Task.FromResult(file.Filename?.ToLower()?.EndsWith(".cia") ?? false);
    }
    {
        var file = new CiaFile(data);
        await file.Initalize();
        return file;
    }
    {
        CiaData = data ?? throw new ArgumentNullException(nameof(data));
    }
    {
        var headerSize = await CiaData.ReadInt32Async(0);
        CiaHeader = new CiaHeader(await CiaData.ReadArrayAsync(0, headerSize));
        var ticketOffset = BitMath.Align(certOffset + CiaHeader.CertificateChainSize, 64);
        var tmdOffset = BitMath.Align(ticketOffset + CiaHeader.TicketSize, 64);
        var contentOffset = BitMath.Align(tmdOffset + CiaHeader.TmdFileSize, 64);
        var metaOffset = BitMath.Align(contentOffset + CiaHeader.ContentSize, 64);
        long partitionStart = contentOffset;
        for (var i = 0; i < TmdMetadata.ContentChunkRecords.Length; i++)
        {
            var chunkRecord = TmdMetadata.ContentChunkRecords[i];
            var partitionLength = chunkRecord.ContentSize;
            int contentIndex = chunkRecord.ContentIndex;
        }
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