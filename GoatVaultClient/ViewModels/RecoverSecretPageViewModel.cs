using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultCore.Models.Shamir;
using GoatVaultCore.Services.Shamir;
using System.Collections.ObjectModel;

namespace GoatVaultClient.ViewModels;

public partial class RecoverSecretViewModel(ShamirSSService st) : BaseViewModel
{
    // ── Input fields ─────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddShareCommand))]
    private string _currentShareInput = string.Empty;

    [ObservableProperty]
    private string _passphrase = string.Empty; // Added for SLIP-39

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _hasRecoveredSecret;

    [ObservableProperty]
    private string _recoveredSecret = string.Empty;

    [ObservableProperty]
    private bool _isSecretVisible;

    // ── Collected shares ─────────────────────────────────────────

    public ObservableCollection<RecoveryShare> CollectedShares { get; } = [];

    public int ShareCount => CollectedShares.Count;
    public bool HasShares => CollectedShares.Count > 0;

    // ── Commands ─────────────────────────────────────────────────

    private bool CanAddShare() => !string.IsNullOrWhiteSpace(CurrentShareInput);

    [RelayCommand(CanExecute = nameof(CanAddShare))]
    private void AddShare()
    {
        var trimmed = CurrentShareInput.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return;

        CollectedShares.Add(new RecoveryShare
        {
            Index = CollectedShares.Count + 1,
            Mnemonic = trimmed
        });

        CurrentShareInput = string.Empty;
        OnPropertyChanged(nameof(ShareCount));
        OnPropertyChanged(nameof(HasShares));
        RecoverCommand.NotifyCanExecuteChanged();

        HasError = false;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void RemoveShare(RecoveryShare? share)
    {
        if (share is null)
            return;
        CollectedShares.Remove(share);

        for (var i = 0; i < CollectedShares.Count; i++)
        {
            CollectedShares[i] = CollectedShares[i] with { Index = i + 1 };
        }

        OnPropertyChanged(nameof(ShareCount));
        OnPropertyChanged(nameof(HasShares));
        RecoverCommand.NotifyCanExecuteChanged();
    }

    private bool CanRecover() => CollectedShares.Count >= 2;

    [RelayCommand(CanExecute = nameof(CanRecover))]
    private async Task RecoverAsync()
    {
        HasError = false;
        ErrorMessage = string.Empty;
        HasRecoveredSecret = false;
        RecoveredSecret = string.Empty;
        IsSecretVisible = false;

        await SafeExecuteAsync(async () =>
        {
            var mnemonics = CollectedShares.Select(s => s.Mnemonic).ToList();

            var result = await Task.Run(() =>
                st.RecoverSecret(mnemonics, Passphrase ?? string.Empty));

            RecoveredSecret = result;
            HasRecoveredSecret = true;
        });
    }

    [RelayCommand]
    private void ToggleSecretVisibility() => IsSecretVisible = !IsSecretVisible;

    [RelayCommand]
    private async Task CopyRecoveredSecretAsync()
    {
        if (string.IsNullOrEmpty(RecoveredSecret))
            return;
        await Clipboard.Default.SetTextAsync(RecoveredSecret);

        _ = Task.Delay(TimeSpan.FromSeconds(60)).ContinueWith(_ =>
            MainThread.BeginInvokeOnMainThread(async () =>
                await Clipboard.Default.SetTextAsync(string.Empty)));
    }

    [RelayCommand]
    private async Task PasteShareAsync()
    {
        var text = await Clipboard.Default.GetTextAsync();
        if (!string.IsNullOrWhiteSpace(text))
            CurrentShareInput = text;
    }

    [RelayCommand]
    private void Reset()
    {
        CurrentShareInput = string.Empty;
        Passphrase = string.Empty;
        CollectedShares.Clear();
        RecoveredSecret = string.Empty;
        HasRecoveredSecret = false;
        IsSecretVisible = false;
        HasError = false;
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(ShareCount));
        OnPropertyChanged(nameof(HasShares));
    }
}