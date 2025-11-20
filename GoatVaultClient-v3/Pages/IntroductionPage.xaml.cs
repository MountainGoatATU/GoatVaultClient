using Microsoft.Maui.Controls;

namespace GoatVaultClient_v3;

public partial class Introduction : ContentPage
{
    private readonly IServiceProvider _services;

    public Introduction(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();
    }

    private async void OnGetStartedClicked(object sender, EventArgs e)
    {
        var registerPage = _services.GetRequiredService<RegisterPage>();
        await Navigation.PushAsync(registerPage);
    }
}