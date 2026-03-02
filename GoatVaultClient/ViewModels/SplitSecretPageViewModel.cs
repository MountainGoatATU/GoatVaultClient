using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultApplication.Shamir;
using GoatVaultClient.Controls.Popups;
using GoatVaultCore.Models.Objects;
using Microsoft.Extensions.Logging;
using Mopups.Services;
using System.Collections.ObjectModel;

namespace GoatVaultClient.ViewModels;

[QueryProperty("Mp", "Mp")]
public partial class SplitSecretViewModel(
    // ── Constructor ──────────────────────────────────────────────
    SplitKeyUseCase splitKeyUseCase,
    EnableShamirUseCase enableShamirUseCase,
    ILogger<SplitSecretViewModel>? logger
    ) : BaseViewModel
{
    // ── Input fields ─────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SplitCommand))]
    private string _mp = string.Empty;

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
    private int _safetyCheckPass = 0;

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
        HasError = false;
        ErrorMessage = string.Empty;
        GeneratedShares.Clear();

        var response = await splitKeyUseCase.Execute(Mp, Passphrase, TotalShares, Threshold);
        foreach (var share in response)
            GeneratedShares.Add(share);

        HasResults = true;
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
        if (_safetyCheckPass == 0)
        {
            var popup = new PromptPopup(
               "Warning",
               "Did you copy the shares and passphrase properly? Without them, you are not going to be able to recover your master password",
               "Yes",
               "No"
               );

            await MopupService.Instance.PushAsync(popup);

            var result = await popup.WaitForScan();
            if (result)
                _safetyCheckPass++;

            await MopupService.Instance.PopAllAsync();
        }
        else if (_safetyCheckPass == 1)
        {
            var popup = new PromptPopup(
               "LAST WARNING",
               "LOSING ACCESS OR HAVING INCORRECTLY COPIED THE SHARES AND PASSPHRASE WILL RESULT IN INNABILITY TO RECOVER YOUR MASTER PASSWORD",
               "I understand",
               "Cancel"
               );

            await MopupService.Instance.PushAsync(popup);
            var result = await popup.WaitForScan();
            if (result)
            {
                await MopupService.Instance.PushAsync(new PendingPopup("Enabling Recovery on your account..."));
                await enableShamirUseCase.ExecuteAsync(Mp);
                await MopupService.Instance.PopAllAsync();
                await Shell.Current.GoToAsync("//gratitude");
            }

            await MopupService.Instance.PopAllAsync();
        }
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