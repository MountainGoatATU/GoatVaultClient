using GoatVaultApplication.Account;
using GoatVaultApplication.Auth;
using GoatVaultApplication.Shamir;
using GoatVaultApplication.Vault;
using GoatVaultClient.Pages;
using GoatVaultClient.ViewModels;
using GoatVaultClient.ViewModels.controls;
using GoatVaultClient.ViewModels.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace GoatVaultClient.Services.Registration;

public static class UseCasesServiceRegistration
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        // Login & Account Use Cases
        services.AddTransient<LoginOfflineUseCase>();
        services.AddTransient<LoginOnlineUseCase>();
        services.AddTransient<LogoutUseCase>();
        services.AddTransient<RegisterUseCase>();
        
        // Shamir Use Cases
        services.AddTransient<DisableShamirUseCase>();
        services.AddTransient<EnableShamirUseCase>();
        services.AddTransient<RecoverKeyUseCase>();
        services.AddTransient<SplitKeyUseCase>();
        services.AddTransient<ValidateUserEmailUseCase>();
        
        // Vault Use Cases
        services.AddTransient<AddVaultEntryUseCase>();
        services.AddTransient<CalculateVaultScoreUseCase>();
        services.AddTransient<DeleteVaultEntryUseCase>();
        services.AddTransient<LoadVaultUseCase>();
        services.AddTransient<SaveVaultUseCase>();
        services.AddTransient<SyncVaultUseCase>();
        services.AddTransient<UpdateVaultEntryUseCase>();
        services.AddTransient<WipeVaultUseCase>();
        
        // Account Management Use Cases
        services.AddTransient<ChangeEmailUseCase>();
        services.AddTransient<ChangePasswordUseCase>();
        services.AddTransient<DisableMfaUseCase>();
        services.AddTransient<EnableMfaUseCase>();
        services.AddTransient<LoadUserProfileUseCase>();
        services.AddTransient<DeleteAccountUseCase>();

        return services;
    }
}
