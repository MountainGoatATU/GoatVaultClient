using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GoatVaultInfrastructure.Services.Logging;

/// <summary>
/// Configuration for the file logger provider.
/// </summary>
public sealed class FileLoggerOptions
{
    /// <summary>
    /// Directory where log files are stored. Defaults to app data directory.
    /// Must be set before the provider is used.
    /// </summary>
    public string LogDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Maximum size of a single log file in bytes. Default: 5MB.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>
    /// Maximum number of log files to retain. Default: 3.
    /// </summary>
    public int MaxRetainedFiles { get; set; } = 3;

    /// <summary>
    /// Minimum log level for the file logger. Default: Information.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
}

/// <summary>
/// Logger provider that writes structured log entries to rotating files.
/// Thread-safe. Logs are written synchronously to a shared file writer with a lock.
/// </summary>
[ProviderAlias("File")]
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly FileLoggerOptions _options;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private readonly FileLogWriter _writer;

    public FileLoggerProvider(FileLoggerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_options.LogDirectory))
            throw new ArgumentException("LogDirectory must be set.", nameof(options));

        Directory.CreateDirectory(_options.LogDirectory);
        _writer = new FileLogWriter(_options);
    }

    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _writer, _options));

    public void Dispose()
    {
        _writer.Dispose();
        _loggers.Clear();
    }
}

/// <summary>
/// Individual logger instance for a specific category.
/// </summary>
internal sealed class FileLogger(string categoryName, FileLogWriter writer, FileLoggerOptions options) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= options.MinimumLevel && logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var level = logLevel switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "???"
        };

        // Extract short category name (last segment after last dot)
        var shortCategory = categoryName;
        var lastDot = categoryName.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < categoryName.Length - 1)
            shortCategory = categoryName[(lastDot + 1)..];

        var logLine = $"[{timestamp}] [{level}] [{shortCategory}] {message}";
        if (exception != null)
            logLine += Environment.NewLine + exception;

        writer.WriteLine(logLine);
    }
}

/// <summary>
/// Thread-safe file writer with rotation support.
/// Rotates when the current file exceeds MaxFileSizeBytes.
/// Retains at most MaxRetainedFiles log files.
/// </summary>
internal sealed class FileLogWriter : IDisposable
{
    private readonly FileLoggerOptions _options;
    private readonly Lock _lock = new();
    private StreamWriter? _currentWriter;
    private string? _currentFilePath;
    private long _currentFileSize;
    private bool _disposed;

    public FileLogWriter(FileLoggerOptions options)
    {
        _options = options;
        OpenNewFile();
    }

    public void WriteLine(string line)
    {
        if (_disposed) return;

        lock (_lock)
        {
            if (_disposed) return;

            try
            {
                var bytes = System.Text.Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;

                if (_currentFileSize + bytes > _options.MaxFileSizeBytes)
                {
                    RotateFile();
                }

                _currentWriter?.WriteLine(line);
                _currentWriter?.Flush();
                _currentFileSize += bytes;
            }
            catch
            {
                // Logging should never crash the app.
                // If file writing fails, silently drop the log entry.
            }
        }
    }

    private void OpenNewFile()
    {
        _currentFilePath = Path.Combine(_options.LogDirectory, $"goatvault-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log");
        _currentWriter = new StreamWriter(_currentFilePath, append: true, encoding: System.Text.Encoding.UTF8)
        {
            AutoFlush = false
        };
        _currentFileSize = new FileInfo(_currentFilePath).Length;
    }

    private void RotateFile()
    {
        _currentWriter?.Flush();
        _currentWriter?.Dispose();

        // Delete the oldest files retention limit exceeded
        PruneOldFiles();

        OpenNewFile();
    }

    private void PruneOldFiles()
    {
        try
        {
            var logFiles = Directory.GetFiles(_options.LogDirectory, "goatvault-*.log")
                .OrderByDescending(f => f)
                .ToList();

            // Keep MaxRetainedFiles - 1 (since we're about to create a new one)
            var filesToDelete = logFiles.Skip(_options.MaxRetainedFiles - 1);
            foreach (var file in filesToDelete)
            {
                try { File.Delete(file); } catch { /* TODO */ }
            }
        }
        catch
        {
            // TODO
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            _currentWriter?.Flush();
            _currentWriter?.Dispose();
        }
    }
}
