using CommunityToolkit.Maui;
using GoatVaultClient_v4.Database;
using GoatVaultClient_v4.Pages;
using GoatVaultClient_v4.Services;
using GoatVaultClient_v4.Services.API;
using GoatVaultClient_v4.Services.Secrets;
using GoatVaultClient_v4.Services.Vault;
using GoatVaultClient_v4.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using SecretSharingDotNet.Math;
using System.Net.Http.Headers;
using System.Numerics;
using UraniumUI;
using Debug = System.Diagnostics.Debug;

namespace GoatVaultClient_v4;

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
            .UseUraniumUIWebComponents()
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

        // Test services
        builder.Services.AddSingleton<FakeDataSource>();

        // TODO: Shamir services
        builder.Services.AddSingleton<IExtendedGcdAlgorithm<BigInteger>, ExtendedEuclideanAlgorithm<BigInteger>>();
        // builder.Services.AddSingleton<IMakeSharesUseCase<BigInteger>, ShamirsSecretSharing<BigInteger>>();
        // builder.Services.AddSingleton<IReconstructionUseCase<BigInteger>, ShamirsSecretSharing<BigInteger>>();

        builder.Services.AddTransient<SecretService>();

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
}