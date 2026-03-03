using CommunityToolkit.Mvvm.ComponentModel;

namespace GoatVaultCore.Models;

public partial class CategoryItem : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private int _entryCount;
}