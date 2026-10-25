using FluentAssertions;
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
}
