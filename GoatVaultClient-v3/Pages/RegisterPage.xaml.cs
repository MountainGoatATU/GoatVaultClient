using System;
using GoatVaultClient_v3.Models;
using GoatVaultClient_v3.Services;
using GoatVaultClient_v3.ViewModels;
using Microsoft.Maui.Controls;

namespace GoatVaultClient_v3
{
    public partial class RegisterPage : ContentPage
    {
        public RegisterPage(RegisterPageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
