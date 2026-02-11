using CommunityToolkit.Maui;
using GoatVaultClient.Pages;
using GoatVaultClient.Services;
using GoatVaultClient.ViewModels;
using GoatVaultClient.ViewModels.controls;
using GoatVaultCore.Services.Secrets;
using GoatVaultInfrastructure.Database;
using GoatVaultInfrastructure.Services;
using GoatVaultInfrastructure.Services.API;
using GoatVaultInfrastructure.Services.Logging;
using GoatVaultInfrastructure.Services.Vault;
using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using SecretSharingDotNet.Math;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection;
using UraniumUI;

namespace GoatVaultClient;

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
                fonts.AddFont("JetBrainsMon-Semibold.ttf", "JetBrainsMonoSemibold");
                fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
                fonts.AddFont("Roboto-Medium.ttf", "RobotoMedium");
                fonts.AddFontAwesomeIconFonts();
                fonts.AddMaterialSymbolsFonts();
                fonts.AddFluentIconFonts();
            });
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

        // App settings configuration
        builder.AddAppSettings();

        // Setup SQLite local database
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "localVault.db");
        var connectionString = $"Data Source={dbPath}";

        builder.Services.AddDbContext<GoatVaultDb>(options =>
            options.UseSqlite(connectionString));

        // Register HttpService with HttpClientFactory
        builder.Services.AddHttpClient<HttpService>(client =>
        {
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (MAUI; Android/iOS/Desktop) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0 Safari/537.36");
        });

        // Register app services
        builder.Services.AddSingleton<VaultService>();
        builder.Services.AddSingleton<UserService>();
        builder.Services.AddSingleton<AuthTokenService>();
        builder.Services.AddSingleton<VaultSessionService>();
        builder.Services.AddSingleton<MarkdownHelperService>();
        builder.Services.AddSingleton<ConnectivityService>();
        builder.Services.AddTransient<IAuthenticationService, AuthenticationService>();
        builder.Services.AddSingleton<ISyncingService, SyncingService>();

        // Test services
        builder.Services.AddSingleton<FakeDataSource>();

        // TODO: Fix Shamir services
        builder.Services.AddSingleton<IExtendedGcdAlgorithm<BigInteger>, ExtendedEuclideanAlgorithm<BigInteger>>();
        // builder.Services.AddSingleton<IMakeSharesUseCase<BigInteger>, ShamirsSecretSharing<BigInteger>>();
        // builder.Services.AddSingleton<IReconstructionUseCase<BigInteger>, ShamirsSecretSharing<BigInteger>>();

        builder.Services.AddTransient<ShamirService>();


        // UraniumUI dialogs
        builder.Services.AddMopupsDialogs();
        builder.Services.AddCommunityToolkitDialogs();

        // Ensure DB creation when app starts
        using (var scope = builder.Services.BuildServiceProvider().CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GoatVaultDb>();
            db.Database.EnsureCreated();
        }

        builder.Services.AddSingleton<GoatTipsService>();
        builder.Services.AddSingleton<TotpManagerService>();
        builder.Services.AddSingleton<CategoryManagerService>();
        builder.Services.AddSingleton<VaultEntryManagerService>();

        // Register pages
        builder.Services.AddTransient<SyncStatusBarViewModel>();
        builder.Services.AddTransient<MainPageViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<IntroductionPageViewModel>();
        builder.Services.AddTransient<IntroductionPage>();
        builder.Services.AddTransient<RegisterPageViewModel>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<LoginPageViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<GratitudePageViewModel>();
        builder.Services.AddTransient<GratitudePage>();
        builder.Services.AddTransient<EducationPageViewModel>();
        builder.Services.AddTransient<EducationPage>();
        builder.Services.AddTransient<EducationDetailViewModel>();
        builder.Services.AddTransient<EducationDetailPage>();
        builder.Services.AddTransient<UserPageViewModel>();
        builder.Services.AddTransient<UserPage>();

        // Build the app
        var app = builder.Build();

        // Initialize database after app is built
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GoatVaultDb>();
            db.Database.EnsureCreated();

            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MauiProgram");
            logger.LogInformation("Database initialized at {DbPath}", dbPath);
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger("MauiProgram");
            logger?.LogCritical(ex, "Failed to initialize database");
        }

        // Done!
        return app;


    }
    extension(MauiAppBuilder builder)
    {
        private void AddJsonSettings(string fileName)
        {
            using var stream = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream($"GoatVaultClient.{fileName}");

            if (stream == null)
                return;

            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
            builder.Configuration.AddConfiguration(config);
        }

        private void AddAppSettings()
        {
            builder.AddJsonSettings("appsettings.json");
        }
    }
}