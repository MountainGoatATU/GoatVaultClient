using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GoatVaultClient.Models.Vault;

public partial class VaultEntry : ObservableObject
{
    [ObservableProperty] [property: Required] private string site;
    [ObservableProperty] [property: Required] private string userName;
    [ObservableProperty] [property: Required] private string password;
    [ObservableProperty] [property: Required] private string description;
    [ObservableProperty] [property: Required] private string category;
}