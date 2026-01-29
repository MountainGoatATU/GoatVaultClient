using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GoatVaultClient.Models.Vault;

public partial class CategoryItem : ObservableObject
{
    [ObservableProperty] [property: Required] private string name;
}