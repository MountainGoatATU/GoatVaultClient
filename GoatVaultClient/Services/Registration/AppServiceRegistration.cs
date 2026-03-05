using GoatVaultApplication.Session;
using GoatVaultClient.Pages;
using GoatVaultClient.ViewModels;
using GoatVaultClient.ViewModels.controls;
using GoatVaultClient.ViewModels.Controls;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Services;
using GoatVaultInfrastructure.Database;
using GoatVaultInfrastructure.Services;
using GoatVaultInfrastructure.Services.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Xecrets.Slip39;

namespace GoatVaultClient.Services.Registration;

public static class AppServiceRegistration
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // Core app services
        services.AddSingleton<ConnectivityService>();
        services.AddSingleton<IOfflineModeService, OfflineModeService>();
        services.AddSingleton<ISyncingService, SyncingService>();
        services.AddSingleton<ISessionContext, SessionContext>();
        services.AddTransient<IUserRepository, UserRepository>();
        services.AddSingleton<ICryptoService, CryptoService>();
        services.AddSingleton<IVaultCrypto, VaultCrypto>();
        services.AddSingleton<IAuthTokenService, AuthTokenService>();
        services.AddSingleton<JwtUtils>();

        // Misc services
        services.AddSingleton<GoatTipsService>();
        services.AddSingleton<TotpManagerService>();
        services.AddTransient<CategoryManagerService>();
        services.AddTransient<VaultEntryManagerService>();
        services.AddTransient<IPwnedPasswordService, PwnedPasswordService>();
        services.AddTransient<IVaultScoreCalculatorService, VaultScoreCalculatorService>();
        services.AddTransient<IPasswordStrengthService, PasswordStrengthService>();
        services.AddTransient<IRandomTipService, RandomTipService>();

        // Shamir Services
        services.AddTransient<IShamirsSecretSharing, ShamirsSecretSharing>();
        services.AddSingleton<IRandom, StrongRandom>();
        services.AddTransient<IShamirSsService, ShamirSsService>();

        // HTTP services
        services.AddTransient<AuthenticatedHttpHandler>(sp =>
        {
            const string refreshEndpoint = "v1/auth/refresh";
            var serverBaseUrl = sp.GetRequiredService<IConfiguration>()["API_BASE_URL"];
            var authService = sp.GetRequiredService<IAuthTokenService>();
            var jwtUtils = sp.GetRequiredService<JwtUtils>();
            var offlineMode = sp.GetRequiredService<IOfflineModeService>();
            var logger = sp.GetService<ILogger<AuthenticatedHttpHandler>>();
            return new AuthenticatedHttpHandler(
                authService,
                jwtUtils,
                offlineMode,
                $"{serverBaseUrl}/{refreshEndpoint}",
                logger
            );
        });

        services.AddHttpClient<IServerAuthService, ServerAuthService>()
            .AddHttpMessageHandler<AuthenticatedHttpHandler>();
        services.AddHttpClient<IHttpService, HttpService>()
            .AddHttpMessageHandler<AuthenticatedHttpHandler>();

        return services;
    }
}
