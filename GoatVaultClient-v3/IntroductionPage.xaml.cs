using Microsoft.Maui.Controls;

namespace GoatVaultClient_v3;

public partial class Introduction : ContentPage
{
    private readonly IServiceProvider _services;

    public Introduction(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    private async void OnGetStartedClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage(_services));
    }
}