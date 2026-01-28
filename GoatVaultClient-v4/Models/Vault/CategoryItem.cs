using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GoatVaultClient_v4.Models.Vault;

public partial class CategoryItem : ObservableObject
{
    [ObservableProperty] [property: Required] private string name;
}