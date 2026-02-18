using GoatVaultClient.Pages;
using GoatVaultClient.ViewModels;
using GoatVaultCore.Services.Shamir;
using GoatVaultCore.Shamir.Services;
using SecretSharingDotNet.Cryptography;
using SecretSharingDotNet.Cryptography.ShamirsSecretSharing;
using SecretSharingDotNet.Math;
using System.Numerics;

namespace GoatVaultClient.Services;

/// <summary>
/// Extension method to register all Shamir Secret Sharing services,
/// ViewModels, and Pages into the MAUI DI container.
///
/// Usage in MauiProgram.cs:
///   builder.Services.AddShamirSecretSharing();
/// </summary>
public static class ShamirServiceRegistration
{
    public static IServiceCollection AddShamirSecretSharing(this IServiceCollection services)
    {
        // Shamir Services
        services.AddSingleton<IExtendedGcdAlgorithm<BigInteger>, ExtendedEuclideanAlgorithm<BigInteger>>();
        services.AddSingleton<IMakeSharesUseCase<BigInteger>, SecretSplitter<BigInteger>>();
        services.AddSingleton<IReconstructionUseCase<BigInteger>, SecretReconstructor<BigInteger>>();

        // ── Core services (singletons — stateless, thread-safe) ──
        services.AddSingleton<IMnemonicEncoder>(sp =>
            WordListLoader.CreateEncoderAsync().GetAwaiter().GetResult());

        services.AddSingleton<IEnvelopeSharingService, EnvelopeSharingService>();

        // ── ViewModels (transient — fresh per page navigation) ───
        services.AddTransient<SplitSecretViewModel>();
        services.AddTransient<RecoverSecretViewModel>();

        // ── Pages ────────────────────────────────────────────────
        services.AddTransient<SplitSecretPage>();
        services.AddTransient<RecoverSecretPage>();

        return services;
    }
}