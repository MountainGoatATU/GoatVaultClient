using GoatVaultInfrastructure.Services;
using Microsoft.Extensions.Logging;

namespace GoatVaultTests.Infrastructure.Services;

public sealed class FileLoggerProviderTests : IDisposable
{
    private readonly List<string> _tempDirectories = [];

    [Fact]
    public void Constructor_WhenLogDirectoryIsMissing_ThrowsArgumentException()
    {
        // Arrange
        const string emptyDirectory = "   ";

        // Act
        var options = new FileLoggerOptions
        {
            LogDirectory = emptyDirectory
        };

        // Assert
        Assert.Throws<ArgumentException>(() => new FileLoggerProvider(options));
    }

    [Fact]
    public void CreateLogger_SameCategory_ReturnsSameInstance()
    {
        // Arrange
        var options = CreateOptions();
        using var provider = new FileLoggerProvider(options);

        // Act
        var logger1 = provider.CreateLogger("My.Category");
        var logger2 = provider.CreateLogger("My.Category");

        // Assert
        Assert.Same(logger1, logger2);
    }

    [Fact]
    public void Log_RespectsMinimumLevel_AndWritesFormattedLine()
    {
        // Arrange
        var options = CreateOptions(minimumLevel: LogLevel.Warning);

        // Act
        using (var provider = new FileLoggerProvider(options))
        {
            var logger = provider.CreateLogger("GoatVault.Tests.Category");

            logger.LogInformation("This should not be written");
            logger.LogWarning("This should be written");
        }

        var content = ReadLogFileText(options.LogDirectory);

        // Assert
        Assert.DoesNotContain("This should not be written", content);
        Assert.Contains("This should be written", content);
        Assert.Contains("[WRN]", content);
        Assert.Contains("[Category]", content);
    }

    [Fact]
    public void Log_WhenExceptionProvided_AppendsExceptionDetails()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        using (var provider = new FileLoggerProvider(options))
        {
            var logger = provider.CreateLogger("GoatVault.Tests.ExceptionCategory");
            var exception = new InvalidOperationException("boom");

            logger.LogError(exception, "Failed operation");
        }

        var content = ReadLogFileText(options.LogDirectory);

        // Assert
        Assert.Contains("Failed operation", content);
        Assert.Contains("System.InvalidOperationException: boom", content);
    }

    public void Dispose()
    {
        foreach (var directory in _tempDirectories)
        {
            try
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
            }
            catch
            {
                // Best effort cleanup for test temp folders.
            }
        }
    }

    private FileLoggerOptions CreateOptions(LogLevel minimumLevel = LogLevel.Information)
    {
        var directory = Path.Combine(Path.GetTempPath(), "goatvault-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        _tempDirectories.Add(directory);

        return new FileLoggerOptions
        {
            LogDirectory = directory,
            MinimumLevel = minimumLevel,
            MaxFileSizeBytes = 1024 * 1024,
            MaxRetainedFiles = 3
        };
    }

    private static string ReadLogFileText(string logDirectory)
    {
        var logFile = Directory.GetFiles(logDirectory, "goatvault-*.log").Single();
        return File.ReadAllText(logFile);
    }
}
