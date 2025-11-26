using CommunityToolkit.Mvvm.ComponentModel;

namespace GoatVaultClient_v3.ViewModels
{
    // 1. Must be 'public partial' for source generators to work
    // 2. Inherits from ObservableObject to handle INotifyPropertyChanged
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool isBusy;

        [ObservableProperty]
        private string title = string.Empty;

        // Helper property for XAML (e.g., IsEnabled="{Binding IsNotBusy}")
        public bool IsNotBusy => !IsBusy;
    }
}