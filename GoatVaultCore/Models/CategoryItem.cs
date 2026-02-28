using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GoatVaultCore.Models;

public partial class CategoryItem : ObservableObject
{
    [ObservableProperty] [property: Required] private string _name = string.Empty;
    public int EntryCount { get; set; }
}