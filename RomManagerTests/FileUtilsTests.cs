using AwesomeAssertions;
using RomManagerShared.Utils;

namespace RomManagerTests;

public class FileUtilsTests
{
    [Fact]
    public void OpenRead_AllowsConcurrentReaders()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, [1, 2, 3]);
            using var first = FileUtils.OpenRead(path);
            using var second = FileUtils.OpenRead(path);

            first.CanWrite.Should().BeFalse();
            second.ReadByte().Should().Be(1);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Log_FromManyThreads_KeepsEveryLine()
    {
        var marker = Guid.NewGuid().ToString("N");
        var log = Path.Combine(Path.GetTempPath(), marker + ".log");
        RomManagerShared.Logging.LogSink.FilePathOverride = log;
        try
        {
            Parallel.For(0, 200, i => FileUtils.Log($"{marker} {i}"));
            File.ReadLines(log).Count(l => l.Contains(marker)).Should().Be(200);
        }
        finally
        {
            RomManagerShared.Logging.LogSink.FilePathOverride = null;
            File.Delete(log);
        }
    }
}
