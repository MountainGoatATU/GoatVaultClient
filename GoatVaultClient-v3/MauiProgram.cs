using System.Diagnostics;
using System.Net.Http.Headers;
using System.Numerics;
using System.Numerics;
using CommunityToolkit.Maui;
using GoatVaultClient_v3.Database;
using GoatVaultClient_v3.Pages;
using GoatVaultClient_v3.Services;
using GoatVaultClient_v3.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using SecretSharingDotNet.Cryptography;
using SecretSharingDotNet.Cryptography;
using SecretSharingDotNet.Math;
using SecretSharingDotNet.Math;
using UraniumUI;

namespace GoatVaultClient_v3
{
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
                .ConfigureMopups()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("JetBrainsMono-Regular.ttf", "JetBrainsMonoRegular");
                    fonts.AddFont("JetBrainsMon-Semibold.ttf", "JetBrainsMonoSemibold");

                    fonts.AddMaterialSymbolsFonts();
                    fonts.AddFontAwesomeIconFonts();
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // Setup SQLite local database
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "localvaultTest.db");
            string connectionString = $"Data Source={dbPath}";
            Debug.WriteLine(dbPath);

            builder.Services.AddDbContext<GoatVaultDB>(options =>
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

            //Test services
            builder.Services.AddSingleton<FakeDataSource>();

            // Shamir services
            builder.Services.AddSingleton<IExtendedGcdAlgorithm<BigInteger>, ExtendedEuclideanAlgorithm<BigInteger>>();
            builder.Services.AddSingleton<IMakeSharesUseCase<BigInteger>, ShamirsSecretSharing<BigInteger>>();
            builder.Services.AddSingleton<IReconstructionUseCase<BigInteger>, ShamirsSecretSharing<BigInteger>>();
            builder.Services.AddTransient<SecretService>();

            // UraniumUI dialogs
            builder.Services.AddMopupsDialogs();
            builder.Services.AddCommunityToolkitDialogs();

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


            // Ensure DB creation when app starts
            using (var scope = builder.Services.BuildServiceProvider().CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GoatVaultDB>();
                db.Database.EnsureCreated();
            }

            return builder.Build();
        }
    }
}
