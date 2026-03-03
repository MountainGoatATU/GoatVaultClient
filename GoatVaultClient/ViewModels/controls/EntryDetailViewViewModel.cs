using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Services;
using GoatVaultCore.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GoatVaultClient.ViewModels.controls;

public partial class EntryDetailViewViewModel(TotpManagerService totpManager) : BaseViewModel
{
    [ObservableProperty] private VaultEntry? _entry;
    [ObservableProperty] private bool _isPasswordVisible = false;

    [RelayCommand]
    private async Task CopyTotpCode()
        => await SafeExecuteAsync(async ()
            => await totpManager.CopyTotpCodeAsync(Entry));
    [RelayCommand]
    private async Task CopyEntry()
        => await SafeExecuteAsync(async ()
            => await VaultEntryManagerService.CopyEntryPasswordAsync(Entry));

    [RelayCommand]
    private void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;
}
