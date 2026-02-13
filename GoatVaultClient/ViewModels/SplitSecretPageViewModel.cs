using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultCore.Models.Shamir;
using GoatVaultCore.Services.Shamir;
using System.Collections.ObjectModel;

namespace GoatVaultClient.ViewModels;

public partial class SplitSecretViewModel : BaseViewModel
{
    private readonly IEnvelopeSharingService _sharingService;

    public SplitSecretViewModel(IEnvelopeSharingService sharingService)
    {
        _sharingService = sharingService;
    }

    // ── Input fields ─────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SplitCommand))]
    private string _secretText = string.Empty;

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

    public ObservableCollection<SharePackage> GeneratedShares { get; } = [];

    // ── Slider ranges ────────────────────────────────────────────

    public int MinShares => 2;
    public int MaxShares => 10;
    public int MinThreshold => 2;

    /// <summary>Threshold can't exceed total shares.</summary>
    public int MaxThreshold => TotalShares;

    // When TotalShares changes, clamp Threshold and notify MaxThreshold
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
            // Run on background thread — Shamir math can be non-trivial
            var packages = await Task.Run(() =>
                _sharingService.Split(SecretText, TotalShares, Threshold));

            foreach (var pkg in packages)
                GeneratedShares.Add(pkg);

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
    private async Task CopyShareAsync(SharePackage? package)
    {
        if (package is null) return;

        // Format: mnemonic words on their own (the user writes these down)
        await Clipboard.Default.SetTextAsync(package.MnemonicShare);

        // Auto-clear clipboard after 60 seconds
        _ = Task.Delay(TimeSpan.FromSeconds(60)).ContinueWith(_ =>
            MainThread.BeginInvokeOnMainThread(async () =>
                await Clipboard.Default.SetTextAsync(string.Empty)));
    }

    [RelayCommand]
    private async Task CopyEnvelopeAsync(SharePackage? package)
    {
        if (package == null) return;

        await Clipboard.Default.SetTextAsync(package.EnvelopeBase64);

        // Clear clipboard after 10 seconds (fire and forget task, but handled safely)
        _ = Task.Run(async () =>
        {
            await Task.Delay(10000);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var current = await Clipboard.Default.GetTextAsync();
                if (!string.IsNullOrEmpty(current))
                {
                    await Clipboard.Default.SetTextAsync(string.Empty);
                }
            });
        });
    }

    [RelayCommand]
    private async Task CopyFullPackageAsync(SharePackage? package)
    {
        if (package is null) return;

        //var text = $"""
        //    === {package.DisplayLabel} ===
        //    Threshold: {package.ThresholdLabel}
            
        //    --- MNEMONIC SHARE (KEEP SECRET) ---
        //    {package.MnemonicShare}
            
        //    --- ENVELOPE (share with all holders) ---
        //    {package.EnvelopeBase64}
        //    """;

        //await Clipboard.Default.SetTextAsync(text);

        //_ = Task.Delay(TimeSpan.FromSeconds(60)).ContinueWith(_ =>
        //    MainThread.BeginInvokeOnMainThread(async () =>
        //        await Clipboard.Default.SetTextAsync(string.Empty)));
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
        TotalShares = 5;
        Threshold = 3;
        GeneratedShares.Clear();
        HasResults = false;
        HasError = false;
        ErrorMessage = string.Empty;
    }
}