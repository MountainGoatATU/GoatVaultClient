using CommunityToolkit.Mvvm.ComponentModel;
using GoatVaultCore.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GoatVaultClient.ViewModels;

[QueryProperty(nameof(VaultEntry), "Entry")]
public partial class EntryDetailsViewModel : BaseViewModel
{
    [ObservableProperty]
    private VaultEntry entry;
}
