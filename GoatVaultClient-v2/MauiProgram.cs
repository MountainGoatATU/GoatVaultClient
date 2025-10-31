﻿using GoatVaultClient_v2.Database;
using GoatVaultClient_v2.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace GoatVaultClient_v2
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            // Setup SQLite local database
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "localvault.db");
            string connectionString = $"Data Source={dbPath}";

            builder.Services.AddDbContext<VaultDB>(options =>
                options.UseSqlite(connectionString));

            // Register HttpService with HttpClientFactory
            builder.Services.AddHttpClient<IHttpService, HttpService>(client =>
            {
                client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (MAUI; Android/iOS/Desktop) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("X-API-KEY", "PB7KTN_edJEz5oUdhTRpaz2T_-SpZj_C5ZvD2AWPcPc");
            });

            // Register app services
            builder.Services.AddSingleton<IVaultService, VaultService>();
            builder.Services.AddSingleton<IUserService, UserService>();

            // ✅ Register your main page (or viewmodel if using MVVM)
            builder.Services.AddSingleton<MainPage>();

            // ✅ Ensure DB creation when app starts
            using (var scope = builder.Services.BuildServiceProvider().CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<VaultDB>();
                db.Database.EnsureCreated();
            }

            return builder.Build();
        }
    }
}
