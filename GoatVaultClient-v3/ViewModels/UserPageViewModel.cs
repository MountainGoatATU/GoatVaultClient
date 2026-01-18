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
        private string userName = "Example User";

        [ObservableProperty]
        private string email = "user@example.com";

        [ObservableProperty]
        private string masterPassword = "password123";

        [RelayCommand]
        private void SaveChanges()
        {
            // Logic to save changes to user profile
        }
    }
}
