using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.ViewModels
{
    public partial class UserPageViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string name = "Example User";

        [ObservableProperty]
        private string email = "user@example.com";

        [ObservableProperty]
        private string masterPassword = "password123";

       
        [RelayCommand]
        private async Task EditNameAsync()
        {
            if (!await AuthorizeAsync())
            {
                await App.Current.MainPage.DisplayAlert("Error", "Incorrect master password", "OK");
                return;
            }

            var newName = await App.Current.MainPage.DisplayPromptAsync("Edit Name", "Enter new name", initialValue: Name);
            if (!string.IsNullOrWhiteSpace(newName))
                Name = newName;
        }

        [RelayCommand]
        private async Task EditEmailAsync()
        {
            if (!await AuthorizeAsync())
            {
                await App.Current.MainPage.DisplayAlert("Error", "Incorrect master password", "OK");
                return;
            }

            var newEmail = await App.Current.MainPage.DisplayPromptAsync("Edit Email", "Enter new email", initialValue: Email);
            if (!string.IsNullOrWhiteSpace(newEmail))
                Email = newEmail;
        }

        [RelayCommand]
        private async Task EditMasterPasswordAsync()
        {
            if (!await AuthorizeAsync())
            {
                await App.Current.MainPage.DisplayAlert("Error", "Incorrect master password", "OK");
                return;
            }

            var newPassword = await App.Current.MainPage.DisplayPromptAsync("Edit Master Password", "Enter new master password");
            if (!string.IsNullOrWhiteSpace(newPassword))
                MasterPassword = newPassword;
        }

        private async Task<bool> AuthorizeAsync()
        {
            var entered = await App.Current.MainPage.DisplayPromptAsync("Authorization", "Enter your master password:", "OK");
            return entered == MasterPassword;
        }
    }
}
