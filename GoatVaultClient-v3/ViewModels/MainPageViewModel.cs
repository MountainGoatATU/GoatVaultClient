using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient_v3.Models;

namespace GoatVaultClient_v3.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        // The list bound to the CollectionView
        [ObservableProperty]
        private ObservableCollection<FolderItem> folders;
        [ObservableProperty]
        private ObservableCollection<PasswordItem> passwords;

        // Displayed in the Header
        [ObservableProperty]
        private string currentContextName = "My Vault";

        private bool _isSortedAscending = true;

        public MainPageViewModel()
        {
            // Dummy Data
            Folders = new ObservableCollection<FolderItem>
            {
                new FolderItem { Name = "Documents" },
                new FolderItem { Name = "Photos" },
                new FolderItem { Name = "Work Projects" },
                new FolderItem { Name = "Recipes" }
            };
            Passwords = new ObservableCollection<PasswordItem>
            {
                new PasswordItem { Description = "Google", UserName = "john.doe@gmail.com", Password = "password123" },
                new PasswordItem { Description = "Netflix", UserName = "chill_user", Password = "secure!Password" },
                new PasswordItem { Description = "GitHub", UserName = "dev_guru", Password = "gitcommitpush" },
                new PasswordItem { Description = "Amazon", UserName = "shopper123", Password = "primeMember!" }
            };
        }

        [RelayCommand]
        private void SortList()
        {
            // Toggle sort order
            var sorted = _isSortedAscending
                ? Folders.OrderBy(f => f.Name).ToList()
                : Folders.OrderByDescending(f => f.Name).ToList();

            Folders = new ObservableCollection<FolderItem>(sorted);
            _isSortedAscending = !_isSortedAscending;
        }

        [RelayCommand]
        private async Task CreateFolder()
        {
            // Using native MAUI DisplayPromptAsync for the input dialog
            // You can replace this with Mopups or a custom Uranium Popup if preferred
            string result = await Shell.Current.DisplayPromptAsync("New Folder", "Enter folder name:");

            if (!string.IsNullOrWhiteSpace(result))
            {
                Folders.Add(new FolderItem
                {
                    Name = result
                });
            }
        }

        [RelayCommand]
        private async Task CopyPassword()
        {

        }

        [RelayCommand]
        private async Task CreatePassword()
        {

        }
    }
}
