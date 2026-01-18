using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient_v3.Controls.Popups;
using Mopups.Services;
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
                return;
            }

            var popup = new AuthorizePopup(title: "Edit Name", isPassword: false, buttonText: "Save");
            await MopupService.Instance.PushAsync(popup);
            while (MopupService.Instance.PopupStack.Contains(popup))
                await Task.Delay(50);

            if (!string.IsNullOrWhiteSpace(popup.Result))
                Name = popup.Result;
        }

        [RelayCommand]
        private async Task EditEmailAsync()
        {
            if (!await AuthorizeAsync())
            {
                return;
            }

            var popup = new AuthorizePopup(title: "Edit Email", isPassword: false, buttonText: "Save");
            await MopupService.Instance.PushAsync(popup);
            while (MopupService.Instance.PopupStack.Contains(popup))
                await Task.Delay(50);

            if (!string.IsNullOrWhiteSpace(popup.Result))
                Email = popup.Result;
        }

        [RelayCommand]
        private async Task EditMasterPasswordAsync()
        {
            if (!await AuthorizeAsync())
            {
                return;
            }

            var popup = new AuthorizePopup(title: "Edit Master Password", isPassword: true, buttonText: "Save");
            await MopupService.Instance.PushAsync(popup);
            while (MopupService.Instance.PopupStack.Contains(popup))
                await Task.Delay(50);

            if (!string.IsNullOrWhiteSpace(popup.Result))
                MasterPassword = popup.Result;
        }

        private async Task<bool> AuthorizeAsync()
        {
            var popup = new AuthorizePopup(title: "Authorization", isPassword: true, buttonText: "OK");
            await MopupService.Instance.PushAsync(popup);

            while (MopupService.Instance.PopupStack.Contains(popup))
                await Task.Delay(50);

            if (popup.Result == null)
                return false;

            if (popup.Result != MasterPassword)
            {
                var errorPopup = new IncorrectPasswordPopup();
                await MopupService.Instance.PushAsync(errorPopup);
                return false;
            }

            return true;
        }
    }
}
