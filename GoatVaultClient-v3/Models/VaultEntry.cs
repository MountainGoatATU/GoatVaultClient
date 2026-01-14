using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GoatVaultClient_v3.Models
{
    public partial class VaultEntry : ObservableObject
    {
        [ObservableProperty]
        [property: Required]
        private string site;
        [ObservableProperty]
        [property: Required]
        private string userName;
        [ObservableProperty]
        [property: Required]
        private string password;
        [ObservableProperty]
        [property: Required]
        private string description;
        [ObservableProperty]
        [property: Required]
        private string category;
    }
}
