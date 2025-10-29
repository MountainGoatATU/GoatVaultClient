using GoatVaultClient.DB;
using GoatVaultClient.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;

namespace GoatVaultClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();

            // SQLite relative path to app folder
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "localvault.db");
            string connectionString = $"Data Source={dbPath}";

            // Register DbContext
            services.AddDbContext<VaultDB>(options =>
                options.UseSqlite(connectionString));

            // Register HttpService with HttpClientFactory
            services.AddHttpClient<HttpService>(client =>
            {
                client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                    "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("X-API-KEY", "PB7KTN_edJEz5oUdhTRpaz2T_-SpZj_C5ZvD2AWPcPc");
            });

            // Register services
            services.AddSingleton<VaultService>();
            services.AddSingleton<UserService>();

            // Register MainWindow
            services.AddTransient<MainWindow>();

            Services = services.BuildServiceProvider();

            // Ensure database is created
            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<VaultDB>();
                db.Database.EnsureCreated(); // creates DB and tables if they don't exist
            }

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}
