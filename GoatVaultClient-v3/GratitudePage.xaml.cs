using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace GoatVaultClient_v3;

public partial class GratitudePage : ContentPage
{
    private readonly IServiceProvider _services;

    public GratitudePage(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();
    }

    private void OnContinueClicked(object sender, EventArgs e)
    {
        var mainPage = _services.GetRequiredService<MainPage>();
        Application.Current.MainPage = new NavigationPage(mainPage);
    }
}