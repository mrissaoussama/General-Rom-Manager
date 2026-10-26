using AwesomeAssertions;
using RomManagerShared.Base.Library;

namespace RomManagerTests;

public sealed class LibraryWatcherTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_root, true);

    private static async Task<bool> WaitAsync(TaskCompletionSource tcs, int ms = 3000) =>
        await Task.WhenAny(tcs.Task, Task.Delay(ms)) == tcs.Task;

    [Fact]
    public async Task Changed_FiresOnceForBurstOfMatchingFiles()
    {
        int count = 0;
        var fired = new TaskCompletionSource();
        using var watcher = new LibraryWatcher(_root, ["nsp"], TimeSpan.FromMilliseconds(300));
        watcher.Changed += () => { Interlocked.Increment(ref count); fired.TrySetResult(); };

        for (int i = 0; i < 5; i++)
            File.WriteAllText(Path.Combine(_root, $"{i}.nsp"), "x");

        (await WaitAsync(fired)).Should().BeTrue();
        await Task.Delay(600, TestContext.Current.CancellationToken);
        count.Should().Be(1);
    }

    [Fact]
    public async Task Changed_IgnoresUnrelatedExtensions()
    {
        var fired = new TaskCompletionSource();
        using var watcher = new LibraryWatcher(_root, ["nsp"], TimeSpan.FromMilliseconds(200));
        watcher.Changed += () => fired.TrySetResult();

        File.WriteAllText(Path.Combine(_root, "notes.txt"), "x");

        (await WaitAsync(fired, 1000)).Should().BeFalse();
    }
}
