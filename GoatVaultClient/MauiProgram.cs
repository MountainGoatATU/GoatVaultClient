using CommunityToolkit.Maui;
using GoatVaultApplication.Account;
using GoatVaultApplication.Auth;
using GoatVaultApplication.Session;
using GoatVaultApplication.Shamir;
using GoatVaultApplication.Vault;
using GoatVaultClient.Pages;
using GoatVaultClient.Services;
using GoatVaultClient.ViewModels;
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

        #region Builder Services

        // Core app services
        builder.Services.AddSingleton<ConnectivityService>();
        builder.Services.AddSingleton<ISyncingService, SyncingService>();
        builder.Services.AddSingleton<ISessionContext, SessionContext>();
        builder.Services.AddTransient<IUserRepository, UserRepository>();
        builder.Services.AddSingleton<ICryptoService, CryptoService>();
        builder.Services.AddSingleton<IVaultCrypto, VaultCrypto>();
        builder.Services.AddSingleton<IAuthTokenService, AuthTokenService>();
        builder.Services.AddSingleton<JwtUtils>();

        // Use cases
        builder.Services.AddTransient<LoginOfflineUseCase>();
        builder.Services.AddTransient<LoginOnlineUseCase>();
        builder.Services.AddTransient<LogoutUseCase>();
        builder.Services.AddTransient<RegisterUseCase>();

        builder.Services.AddTransient<DisableShamirUseCase>();
        builder.Services.AddTransient<EnableShamirUseCase>();
        builder.Services.AddTransient<RecoverKeyUseCase>();
        builder.Services.AddTransient<SplitKeyUseCase>();

        builder.Services.AddTransient<AddVaultEntryUseCase>();
        builder.Services.AddTransient<CalculateVaultScoreUseCase>();
        builder.Services.AddTransient<DeleteVaultEntryUseCase>();
        builder.Services.AddTransient<LoadVaultUseCase>();
        builder.Services.AddTransient<SaveVaultUseCase>();
        builder.Services.AddTransient<SyncVaultUseCase>();
        builder.Services.AddTransient<UpdateVaultEntryUseCase>();

        builder.Services.AddTransient<ChangeEmailUseCase>();
        builder.Services.AddTransient<ChangePasswordUseCase>();
        builder.Services.AddTransient<DisableMfaUseCase>();
        builder.Services.AddTransient<EnableMfaUseCase>();
        builder.Services.AddTransient<LoadUserProfileUseCase>();

        // Misc services
        builder.Services.AddSingleton<GoatTipsService>();
        builder.Services.AddSingleton<TotpManagerService>();
        builder.Services.AddTransient<CategoryManagerService>();
        builder.Services.AddTransient<VaultEntryManagerService>();
        builder.Services.AddTransient<IPwnedPasswordService, PwnedPasswordService>();
        builder.Services.AddTransient<IVaultScoreCalculatorService, VaultScoreCalculatorService>();
        builder.Services.AddTransient<IPasswordStrengthService, PasswordStrengthService>();
        builder.Services.AddTransient<IAuthenticationService, AuthenticationService>();
        builder.Services.AddTransient<IRandomTipService, RandomTipService>();
        // Shamir Test
        builder.Services.AddSingleton<IShamirsSecretSharing, ShamirsSecretSharing>();
        builder.Services.AddSingleton<IRandom, StrongRandom>();
        builder.Services.AddTransient<IShamirSsService,ShamirSsService>();

        // TODO: Fix Shamir services
        builder.Services.AddTransient<SplitSecretViewModel>();
        builder.Services.AddTransient<SplitSecretPage>();
        builder.Services.AddTransient<RecoverSecretViewModel>();
        builder.Services.AddTransient<RecoverSecretPage>();

        // UraniumUI
        builder.Services.AddMopupsDialogs();
        builder.Services.AddCommunityToolkitDialogs();

        #endregion

        builder.Services.AddTransient<AuthenticatedHttpHandler>(sp =>
        {
            const string refreshEndpoint = "v1/auth/refresh";
            var serverBaseUrl = sp.GetRequiredService<IConfiguration>()["API_BASE_URL"];
            var authService = sp.GetRequiredService<IAuthTokenService>();
            var jwtUtils = sp.GetRequiredService<JwtUtils>();
            var logger = sp.GetService<ILogger<AuthenticatedHttpHandler>>();
            return new AuthenticatedHttpHandler(
                authService,
                jwtUtils,
                $"{serverBaseUrl}/{refreshEndpoint}",
                logger
            );
        });

        builder.Services.AddHttpClient<IServerAuthService, ServerAuthService>()
            .AddHttpMessageHandler<AuthenticatedHttpHandler>();
        builder.Services.AddHttpClient<IHttpService, HttpService>()
            .AddHttpMessageHandler<AuthenticatedHttpHandler>();

        #region App pages & view models

        builder.Services.AddTransient<SyncStatusBarViewModel>();
        builder.Services.AddTransient<MainPageViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<OnboardingPageViewModel>();
        builder.Services.AddTransient<OnboardingPage>();
        builder.Services.AddTransient<RegisterPageViewModel>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<LoginPageViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<GratitudePageViewModel>();
        builder.Services.AddTransient<GratitudePage>();
        builder.Services.AddTransient<SecurityPageViewModel>();
        builder.Services.AddTransient<SecurityPage>();
        builder.Services.AddTransient<SettingsPageViewModel>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<EntryDetailPage>();
        builder.Services.AddTransient<EntryDetailsViewModel>();  
        builder.Services.AddTransient<AppShellViewModel>();

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
