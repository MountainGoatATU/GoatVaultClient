using GoatVaultClient.Pages;
using GoatVaultClient.ViewModels;
using GoatVaultClient.ViewModels.controls;
using GoatVaultClient.ViewModels.Controls;
using SecretSharingDotNet.Cryptography;
using SecretSharingDotNet.Cryptography.ShamirsSecretSharing;
using SecretSharingDotNet.Math;

namespace GoatVaultClient.Services.Registration;

public static class PageServiceRegistration
{
    public static IServiceCollection AddPagesAndViewModels(this IServiceCollection services)
    {
        // Sync Status Bar
        services.AddTransient<SyncStatusBarViewModel>();
        
        // Main Page
        services.AddTransient<MainPageViewModel>();
        services.AddTransient<MainPage>();
        
        // Onboarding Page
        services.AddTransient<OnboardingPageViewModel>();
        services.AddTransient<OnboardingPage>();
        
        // Register Page
        services.AddTransient<RegisterPageViewModel>();
        services.AddTransient<RegisterPage>();
        
        // Login Page
        services.AddTransient<LoginPageViewModel>();
        services.AddTransient<LoginPage>();
        
        // Gratitude Page
        services.AddTransient<GratitudePageViewModel>();
        services.AddTransient<GratitudePage>();
        
        // Security Page
        services.AddTransient<SecurityPageViewModel>();
        services.AddTransient<SecurityPage>();
        
        // Settings Page
        services.AddTransient<SettingsPageViewModel>();
        services.AddTransient<SettingsPage>();
        
        // Entry Details Page
        services.AddTransient<EntryDetailPage>();
        services.AddTransient<EntryDetailsViewModel>();
        
        // App Shell ViewModel
        services.AddTransient<AppShellViewModel>();
        
        // Entry Details View ViewModel
        services.AddTransient<EntryDetailViewViewModel>();
        
        // Split Secret Page
        services.AddTransient<SplitSecretViewModel>();
        services.AddTransient<SplitSecretPage>();
        
        // Recover Secret Page
        services.AddTransient<RecoverSecretViewModel>();
        services.AddTransient<RecoverSecretPage>();

        return services;
    }
}
