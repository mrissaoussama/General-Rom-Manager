using Microsoft.Extensions.Logging;

namespace RomManagerShared.Logging;

/// <summary>Writes log entries to <see cref="LogSink"/> (log file, live log panel, parse diagnostics).</summary>
public sealed class LogFileProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName);

    public void Dispose() { }

    private sealed class FileLogger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            LogSink.Write(logLevel, category, formatter(state, exception), exception);
        }
    }
}
