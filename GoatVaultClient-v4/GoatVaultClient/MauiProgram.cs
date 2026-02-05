using CommunityToolkit.Maui;
using GoatVaultInfrastructure.Database;
using GoatVaultClient.Pages;
using GoatVaultClient.Services;
using GoatVaultClient.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using SecretSharingDotNet.Math;
using System.Net.Http.Headers;
using System.Numerics;
using GoatVaultCore.Services.Secrets;
using GoatVaultInfrastructure.Services;
using GoatVaultInfrastructure.Services.API;
using GoatVaultInfrastructure.Services.Vault;
using UraniumUI;
using Debug = System.Diagnostics.Debug;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using SkiaSharp.Views.Maui.Controls.Hosting;
using LiveChartsCore.SkiaSharpView.Maui;

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
            .UseSkiaSharp()
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
#if DEBUG
        builder.Logging.AddDebug();
#endif
        // App settings configuration
        builder.AddAppSettings();

        // Setup SQLite local database
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "localVaultTest.db");
        var connectionString = $"Data Source={dbPath}";
        Debug.WriteLine(dbPath);

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

        // Register pages
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
            Debug.WriteLine("Database initialized successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing database: {ex}");
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