using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient_v3.Models;
using Mopups.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.ViewModels
{
    public partial class NewPasswordPopupViewModel : ObservableObject
    {
        [ObservableProperty]
        private string site = string.Empty;

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string category = "Default";

        [RelayCommand]
        private async Task Cancel()
        {
            await MopupService.Instance.PopAsync();
        }

        [RelayCommand]
        private async Task Create()
        {
            if (!string.IsNullOrWhiteSpace(Site) &&
                !string.IsNullOrWhiteSpace(Username) &&
                !string.IsNullOrWhiteSpace(Password))
            {
                if (App.Current.MainPage.BindingContext is MainPageViewModel mainVm)
                {
                    mainVm.Passwords.Add(new VaultEntry
                    {
                        Site = Site,
                        Username = Username,
                        Password = Password,
                        Category = string.IsNullOrWhiteSpace(Category) ? "Default" : Category
                    });
                }
                await MopupService.Instance.PopAsync();
            }
        }
    }
}
