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
        // appsettings configuration
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
        builder.Services.AddSingleton<MainPageViewModel>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<IntroductionPageViewModel>();
        builder.Services.AddSingleton<IntroductionPage>();
        builder.Services.AddSingleton<RegisterPageViewModel>();
        builder.Services.AddSingleton<RegisterPage>();
        builder.Services.AddSingleton<LoginPageViewModel>();
        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddSingleton<GratitudePageViewModel>();
        builder.Services.AddSingleton<GratitudePage>();
        builder.Services.AddSingleton<EducationPageViewModel>();
        builder.Services.AddSingleton<EducationPage>();
        builder.Services.AddSingleton<EducationDetailViewModel>();
        builder.Services.AddSingleton<EducationDetailPage>();
        builder.Services.AddSingleton<UserPageViewModel>();
        builder.Services.AddSingleton<UserPage>();

        // Done!
        return builder.Build();
    }
    private static void AddJsonSettings(this MauiAppBuilder builder, string fileName)
    {
        using Stream stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream($"GoatVaultClient.{fileName}");
        if (stream != null)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
            builder.Configuration.AddConfiguration(config);
        }
    }
    private static void AddAppSettings(this MauiAppBuilder builder)
    {
        builder.AddJsonSettings("appsettings.json");
#if DEBUG
        builder.AddJsonSettings("appsettings.dev.json");
#endif

#if !DEBUG
        builder.AddJsonSettings("appsettings.prod.json");
#endif

    }
}