﻿namespace DotNet3dsToolkit.Ctr;

public class SingleNcchPartitionContainer : INcchPartitionContainer, IDisposable
{
    public SingleNcchPartitionContainer(NcchPartition partition, int partitionIndex = 0)
    {
        if (partitionIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(partitionIndex), "Partition index must be 0 or greater");
        }
        Partitions[partitionIndex] = partition ?? throw new ArgumentNullException(nameof(partition));
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