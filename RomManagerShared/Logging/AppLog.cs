using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace RomManagerShared.Logging;

/// <summary>
/// Logging for the core library, on top of Microsoft.Extensions.Logging. <see cref="Factory"/> defaults to the
/// log file (<see cref="LogFileProvider"/>); the app replaces it with its host's factory. The category of
/// <see cref="Info"/>/<see cref="Warn"/>/<see cref="Error"/> is the calling file's name (e.g. "PS4PKGParser").
/// </summary>
public static class AppLog
{
    private static ILoggerFactory _factory = CreateDefaultFactory();
    private static readonly ConcurrentDictionary<string, ILogger> Loggers = new();

    public static ILoggerFactory Factory
    {
        get => _factory;
        set
        {
            _factory = value;
            Loggers.Clear();
        }
    }

    public static ILoggerFactory CreateDefaultFactory() =>
        LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Information).AddProvider(new LogFileProvider()));

    public static ILogger For(string category) => Loggers.GetOrAdd(category, c => _factory.CreateLogger(c));

    public static ILogger For<T>() => For(typeof(T).Name);

    public static void Info(string message, [CallerFilePath] string file = "") =>
        For(Category(file)).LogInformation(message);

    public static void Warn(string message, [CallerFilePath] string file = "") =>
        For(Category(file)).LogWarning(message);

    public static void Error(string message, Exception? exception = null, [CallerFilePath] string file = "") =>
        For(Category(file)).LogError(exception, message);

    // Works for Windows and Unix build paths alike.
    private static string Category(string file) =>
        Path.GetFileNameWithoutExtension(file.Replace('\\', '/').Split('/')[^1]);
}
