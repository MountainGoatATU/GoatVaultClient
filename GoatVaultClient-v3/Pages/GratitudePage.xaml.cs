using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using GoatVaultClient_v3.ViewModels;

namespace GoatVaultClient_v3;

public partial class GratitudePage : ContentPage
{
    public GratitudePage(GratitudePageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}