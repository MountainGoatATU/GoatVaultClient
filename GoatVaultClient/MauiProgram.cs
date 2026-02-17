using CommunityToolkit.Maui;
using GoatVaultClient.Pages;
using GoatVaultClient.Services;
using GoatVaultClient.ViewModels;
using GoatVaultClient.ViewModels.controls;
using GoatVaultInfrastructure.Database;
using GoatVaultInfrastructure.Services;
using GoatVaultInfrastructure.Services.API;
using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Reflection;
using GoatVaultApplication.Auth;
using GoatVaultApplication.Session;
using UraniumUI;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Services;

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
                fonts.AddFont("JetBrainsMon-Semibold.ttf", "JetBrainsMonoSemibold");
                fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
                fonts.AddFont("Roboto-Medium.ttf", "RobotoMedium");
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
        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        #region Builder Services

        // Core app services
        builder.Services.AddSingleton<MarkdownHelperService>();
        builder.Services.AddSingleton<ConnectivityService>();
        builder.Services.AddSingleton<ISyncingService, SyncingService>();
        builder.Services.AddScoped<ISessionContext, SessionContext>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ICryptoService, CryptoService>();
        builder.Services.AddScoped<IVaultCrypto, VaultCrypto>();
        builder.Services.AddScoped<IServerAuthService, ServerAuthService>();
        builder.Services.AddSingleton<IAuthTokenService, AuthTokenService>();
        builder.Services.AddSingleton<JwtUtils>();

        // Use cases
        builder.Services.AddScoped<LoginOfflineUseCase>();
        builder.Services.AddScoped<LoginOnlineUseCase>();
        builder.Services.AddScoped<LogoutUseCase>();
        builder.Services.AddScoped<RegisterUseCase>();

        // Misc services
        builder.Services.AddSingleton<GoatTipsService>();
        builder.Services.AddSingleton<TotpManagerService>();
        builder.Services.AddSingleton<CategoryManagerService>();
        builder.Services.AddSingleton<VaultEntryManagerService>();
        builder.Services.AddSingleton<PwnedPasswordService>();

        // Test / helper services
        builder.Services.AddSingleton<FakeDataSource>();
        // builder.Services.AddSingleton<IExtendedGcdAlgorithm<BigInteger>, ExtendedEuclideanAlgorithm<BigInteger>>();
        // builder.Services.AddSingleton<IMakeSharesUseCase<BigInteger>, ShamirsSecretSharing<BigInteger>>();
        // builder.Services.AddSingleton<IReconstructionUseCase<BigInteger>, ShamirsSecretSharing<BigInteger>>();
        // builder.Services.AddTransient<ShamirService>();

        // UraniumUI
        builder.Services.AddMopupsDialogs();
        builder.Services.AddCommunityToolkitDialogs();

        #endregion

        // HTTP
        var serverBaseUrl = builder.Configuration["API_BASE_URL"];
        builder.Services.AddHttpClient<IHttpService, HttpService>()
            .AddHttpMessageHandler(sp =>
            {
                var authService = sp.GetRequiredService<IAuthTokenService>();
                var jwtUtils = sp.GetRequiredService<JwtUtils>();
                var logger = sp.GetService<ILogger<AuthenticatedHttpHandler>>();
                return new AuthenticatedHttpHandler(
                    authService,
                    jwtUtils,
                    $"{serverBaseUrl}v1/auth/refresh",
                    logger
                );
            });

        #region App pages & view models

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

        #endregion

        // Build & ensure DB
        var app = builder.Build();
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();

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