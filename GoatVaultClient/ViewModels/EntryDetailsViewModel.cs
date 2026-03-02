using CommunityToolkit.Mvvm.ComponentModel;
using GoatVaultCore.Models;

namespace GoatVaultClient.ViewModels;

[QueryProperty(nameof(VaultEntry), "Entry")]
public partial class EntryDetailsViewModel : BaseViewModel
{
    [ObservableProperty] private VaultEntry _entry = new();
}
