using CommunityToolkit.Mvvm.ComponentModel;

namespace GoatVaultClient.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsNotBusy))] private bool _isBusy;
    [ObservableProperty] private string _title = string.Empty;

    // Helper property for XAML (e.g., IsEnabled="{Binding IsNotBusy}")
    public bool IsNotBusy => !IsBusy;
}