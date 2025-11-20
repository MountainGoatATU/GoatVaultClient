using Microsoft.Maui.Controls;

namespace GoatVaultClient_v3;

public partial class IntroductionPage : ContentPage
{
    private readonly IServiceProvider _services;

    public IntroductionPage(IServiceProvider services)
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