using GoatVaultClient_v3.Database;
using GoatVaultClient_v3.Services;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using UraniumUI;
using SecretSharingDotNet.Cryptography;
using SecretSharingDotNet.Math;
using System.Numerics;

namespace GoatVaultClient_v3
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseUraniumUI()
                .UseUraniumUIMaterial()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

                    fonts.AddMaterialSymbolsFonts();
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            //Setup SQLite local database
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "localvault.db");
            string connectionString = $"Data Source={dbPath}";

            builder.Services.AddDbContext<GoatVaultDB>(options =>
               options.UseSqlite(connectionString));

            // Register HttpService with HttpClientFactory
            builder.Services.AddHttpClient<HttpService>(client =>
            {
                client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (MAUI; Android/iOS/Desktop) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("X-API-KEY", "PB7KTN_edJEz5oUdhTRpaz2T_-SpZj_C5ZvD2AWPcPc");
            });
       
            //Register app services
            builder.Services.AddSingleton<VaultService>();
            builder.Services.AddSingleton<UserService>();

            //Shamir services
            builder.Services.AddSingleton<IExtendedGcdAlgorithm<BigInteger>, ExtendedEuclideanAlgorithm<BigInteger>>();
            builder.Services.AddSingleton<IMakeSharesUseCase<BigInteger>, ShamirsSecretSharing<BigInteger>>();
            builder.Services.AddSingleton<IReconstructionUseCase<BigInteger>, ShamirsSecretSharing<BigInteger>>();
            builder.Services.AddTransient<SecretService>();

            //Register your main page (or viewmodel if using MVVM)
            builder.Services.AddSingleton<MainPage>();

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
