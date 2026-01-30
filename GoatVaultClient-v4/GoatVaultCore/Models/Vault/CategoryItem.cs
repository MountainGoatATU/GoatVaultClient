using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GoatVaultCore.Models.Vault;

public partial class CategoryItem : ObservableObject
{
    [ObservableProperty] [property: Required] private string name;
}