using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Services;
using GoatVaultClient.ViewModels.controls;
using GoatVaultCore.Models;
using System.Collections.ObjectModel;

namespace GoatVaultClient.ViewModels;

[QueryProperty("Entry", "Entry")]
[QueryProperty("Categories", "Categories")]
public partial class EntryDetailsViewModel : BaseViewModel
{
    private VaultEntryManagerService _vaultEntryManager { get; set; }
    public EntryDetailViewViewModel DetailVM { get; }

    [ObservableProperty]
    private VaultEntry _entry;
    [ObservableProperty]
    private ObservableCollection<CategoryItem> _categories;

    public EntryDetailsViewModel(EntryDetailViewViewModel detailVM, VaultEntryManagerService vaultEntryManager)
    {
        _vaultEntryManager = vaultEntryManager;
        DetailVM = detailVM;
    }

    partial void OnEntryChanged(VaultEntry value)
    {
        DetailVM.Entry = value;
    }

    [RelayCommand]
    private async Task EditEntry()
    {
        if (Entry != null)
        {
            await SafeExecuteAsync(async () =>
            {
                await _vaultEntryManager.EditEntryAsync(Entry, Categories);
            });
        }

    }

    [RelayCommand]
    private async Task DeleteEntry()
    {
        if (Entry != null)
        {
            await SafeExecuteAsync(async () =>
            {
                await _vaultEntryManager.DeleteEntryAsync(Entry);
            });
        }
    }
}
