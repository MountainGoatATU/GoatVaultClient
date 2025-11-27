using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.ViewModels
{
    public partial class NewFolderPopupViewModel : ObservableObject
    {
        [ObservableProperty]
        private string folderName = string.Empty;

        [RelayCommand]
        private async Task Cancel()
        {
            await MopupService.Instance.PopAsync();
        }

        [RelayCommand]
        private async Task Create()
        {
            if (!string.IsNullOrWhiteSpace(FolderName))
            {
                if (App.Current.MainPage.BindingContext is MainPageViewModel mainVm)
                {
                    mainVm.Categories.Add(FolderName.Trim());
                }
                await MopupService.Instance.PopAsync();
            }
        }
    }
}
