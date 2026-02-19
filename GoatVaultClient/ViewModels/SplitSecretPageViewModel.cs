using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultCore.Models.Shamir;
using GoatVaultCore.Services.Shamir;
using System.Collections.ObjectModel;

namespace GoatVaultClient.ViewModels;

public partial class SplitSecretViewModel(ShamirSSService st) : BaseViewModel
{
    // ── Input fields ─────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SplitCommand))]
    private string _secretText = string.Empty;

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

    public ObservableCollection<RecoveryShare> GeneratedShares { get; } = [];

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
        !string.IsNullOrWhiteSpace(SecretText)
        && TotalShares >= 2
        && Threshold >= 2
        && Threshold <= TotalShares;

    [RelayCommand(CanExecute = nameof(CanSplit))]
    private async Task SplitAsync()
    {
        IsBusy = true;
        HasError = false;
        ErrorMessage = string.Empty;
        GeneratedShares.Clear();

        try
        {
            // SLIP-39 generation
            var mnemonics = await Task.Run(() =>
                st.SplitSecret(SecretText, Passphrase ?? string.Empty, TotalShares, Threshold));

            for (var i = 0; i < mnemonics.Count; i++)
            {
                GeneratedShares.Add(new RecoveryShare
                {
                    Index = i + 1,
                    Mnemonic = mnemonics[i]
                });
            }

            HasResults = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            HasError = true;
            HasResults = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CopyShareAsync(RecoveryShare? share)
    {
        if (share is null) return;

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
                    throw; // TODO handle exception
                }
            }));
    }

    [RelayCommand]
    public async Task RecoverSecret()
    {
        await Shell.Current.GoToAsync("//recoverKey");
    }

    [RelayCommand]
    private void Reset()
    {
        SecretText = string.Empty;
        Passphrase = string.Empty;
        TotalShares = 5;
        Threshold = 3;
        GeneratedShares.Clear();
        HasResults = false;
        HasError = false;
        ErrorMessage = string.Empty;
    }
}