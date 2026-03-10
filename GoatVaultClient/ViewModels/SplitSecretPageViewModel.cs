using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultApplication.Auth;
using GoatVaultApplication.Shamir;
using GoatVaultClient.Controls.Popups;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.Objects;
using Microsoft.Extensions.Logging;
using Mopups.Services;
using System.Collections.ObjectModel;
using Email = GoatVaultCore.Models.Objects.Email;

namespace GoatVaultClient.ViewModels;

[QueryProperty("Mp", "Mp")]
[QueryProperty("Email", "Email")]
public partial class SplitSecretViewModel(
    // ── Constructor ──────────────────────────────────────────────
    SplitKeyUseCase splitKeyUseCase,
    EnableShamirUseCase enableShamirUseCase,
    RegisterUseCase registerUseCase,
    ISessionContext session,
    ILogger<SplitSecretViewModel>? logger
    ) : BaseViewModel
{
    // ── Input fields ─────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SplitCommand))]
    private string _mp = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _passphrase = string.Empty; // Added for SLIP-39

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SplitCommand))]
    private int _totalShares = 5;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SplitCommand))]
    private int _threshold = 3;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _hasResults;

    // ── Output ───────────────────────────────────────────────────

    public ObservableCollection<RecoveryShare> GeneratedShares { get; set; } = [];

    // ── Slider ranges ────────────────────────────────────────────

    public int MinShares => 2;
    public int MaxShares => 10;
    public int MinThreshold => 2;

    public int MaxThreshold => TotalShares;

    partial void OnTotalSharesChanged(int value)
    {
        if (Threshold > value)
            Threshold = value;
        OnPropertyChanged(nameof(MaxThreshold));
    }

    // ── Commands ─────────────────────────────────────────────────

    private bool CanSplit() =>
        !string.IsNullOrWhiteSpace(Mp)
        && TotalShares >= 2
        && Threshold >= 2
        && Threshold <= TotalShares;

    [RelayCommand(CanExecute = nameof(CanSplit))]
    private async Task SplitAsync()
    {
        await SafeExecuteAsync(async () =>
        {
            logger?.LogInformation("Starting SplitAsync - TotalShares: {TotalShares}, Threshold: {Threshold}", TotalShares, Threshold);

            HasError = false;
            ErrorMessage = string.Empty;
            GeneratedShares.Clear();

            var response = await splitKeyUseCase.Execute(Mp, Passphrase, TotalShares, Threshold);

            logger?.LogInformation("Successfully generated {ShareCount} shares", response.Count);

            foreach (var share in response)
                GeneratedShares.Add(share);

            HasResults = true;

            logger?.LogInformation("SplitAsync completed successfully");
        });
    }

    [RelayCommand]
    private async Task CopyShareAsync(RecoveryShare? share)
    {
        if (share is null)
            return;

        await Clipboard.Default.SetTextAsync(share.Mnemonic);

        _ = Task.Delay(TimeSpan.FromSeconds(60)).ContinueWith(_ =>
            MainThread.BeginInvokeOnMainThread(async void () =>
            {
                try
                {
                    await Clipboard.Default.SetTextAsync(string.Empty);
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Error during CopyShareAsync() of SplitSecretViewModel");
                    throw;
                }
            }));
    }

    [RelayCommand]
    public static async Task RecoverSecret() => await Shell.Current.GoToAsync("//recoverKey");

    [RelayCommand]
    public async Task Continue()
    {
        var firstPopup = new PromptPopup(
           "Warning",
           "Did you copy the shares and passphrase properly? Without them, you are not going to be able to recover your master password",
           "Yes",
           "No"
           );

        await MopupService.Instance.PushAsync(firstPopup);
        await firstPopup.WaitForScan();

        await MopupService.Instance.PopAllAsync();
        var secondPopup = new PromptPopup(
           "LAST WARNING",
           "LOSING ACCESS OR HAVING INCORRECTLY COPIED THE SHARES AND PASSPHRASE WILL RESULT IN INNABILITY TO RECOVER YOUR MASTER PASSWORD",
           "I understand",
           "Cancel"
           );

        await MopupService.Instance.PushAsync(secondPopup);
        var result = await secondPopup.WaitForScan();
        if (result)
        {
            try
            {
                await MopupService.Instance.PushAsync(new PendingPopup("Enabling Recovery on your account..."));
                
                if (session.UserId != null)
                {
                    await registerUseCase.ExecuteAsync(new Email(Email), Mp, true);
                    // Popup the loading popup
                    await MopupService.Instance.PopAllAsync();
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Shell.Current.GoToAsync("//gratitude");
                }
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Error during Continue() of SplitSecretViewModel");
                await MopupService.Instance.PopAllAsync();
                var errorPopup = new ErrorPopup(
                    "An error occurred while enabling recovery on your account. Please try again."
                    );
                await MopupService.Instance.PushAsync(errorPopup);
            }

        }

        await MopupService.Instance.PopAllAsync();
    }

    [RelayCommand]
    private void Reset()
    {
        Passphrase = string.Empty;
        TotalShares = 5;
        Threshold = 3;
        GeneratedShares.Clear();
        HasResults = false;
        HasError = false;
        ErrorMessage = string.Empty;
    }
}