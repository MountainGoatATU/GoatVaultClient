using CommunityToolkit.Maui;
using GoatVaultApplication.Account;
using GoatVaultApplication.Auth;
using GoatVaultApplication.Session;
using GoatVaultApplication.Shamir;
using GoatVaultApplication.Vault;
using GoatVaultClient.Pages;
using GoatVaultClient.Services;
using GoatVaultClient.Services.Registration;
using GoatVaultClient.ViewModels;
using GoatVaultClient.ViewModels.controls;
using GoatVaultClient.ViewModels.Controls;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Services;
using GoatVaultInfrastructure.Database;
using GoatVaultInfrastructure.Services;
using GoatVaultInfrastructure.Services.Api;
using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Reflection;
using UraniumUI;
using Xecrets.Slip39;

namespace GoatVaultClient;

public static class MauiAppBuilderExtensions
{
    public static MauiAppBuilder AddEmbeddedAppSettings(this MauiAppBuilder builder, string fileName = "appsettings.json")
    {
        // Load embedded JSON
        using var stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream($"GoatVaultClient.{fileName}");
        if (stream != null)
        {
            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
            builder.Configuration.AddConfiguration(config);
        }

        // Override with Preferences (runtime override)
        var serverUrl = Preferences.Get("API_BASE_URL", builder.Configuration["API_BASE_URL"]);
        builder.Configuration["API_BASE_URL"] = serverUrl;

        return builder;
    }
}

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .UseUraniumUIBlurs()
            .ConfigureMopups()
            .UseSkiaSharp()
            .UseLiveCharts()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("JetBrainsMono-Regular.ttf", "JetBrainsMonoRegular");
                fonts.AddFont("JetBrainsMono-Semibold.ttf", "JetBrainsMonoSemibold");
                fonts.AddFontAwesomeIconFonts();
                fonts.AddMaterialSymbolsFonts();
                fonts.AddFluentIconFonts();
            });

        #region Logging

        // Configure logging
        var logDirectory = Path.Combine(FileSystem.AppDataDirectory, "logs");
#if DEBUG
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        builder.Logging.AddDebug();
#else
        builder.Logging.SetMinimumLevel(LogLevel.Information);
#endif
        builder.Logging.AddProvider(new FileLoggerProvider(new FileLoggerOptions
        {
            LogDirectory = logDirectory,
            MaxFileSizeBytes = 5 * 1024 * 1024, // 5MB
            MaxRetainedFiles = 3,
#if DEBUG
            MinimumLevel = LogLevel.Debug
#else
            MinimumLevel = LogLevel.Information
#endif
        }));

        #endregion

        // Embedded config
        builder.AddEmbeddedAppSettings();

        // SQLite DB
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "localVault.db");
        var connectionString = $"Data Source={dbPath}";
        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString), ServiceLifetime.Transient);

        // UraniumUI
        builder.Services.AddMopupsDialogs();
        builder.Services.AddCommunityToolkitDialogs();

        #region Service Registration
        // Core services
        builder.Services.AddAppServices();

        // Use cases
        builder.Services.AddUseCases();

        // Pages & ViewModels
        builder.Services.AddPagesAndViewModels();
        #endregion

        // Build & ensure DB
        var app = builder.Build();
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();

            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MauiProgram");
            logger.LogInformation("Database initialized at {DbPath}", dbPath);
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger("MauiProgram");
            logger?.LogCritical(ex, "Failed to initialize database");
        }

        return app;
    }
}
