using Microsoft.Maui.Controls;

namespace GoatVaultClient_v3;

public partial class Introduction : ContentPage
{
    public Introduction()
    {
        InitializeComponent();
    }

    private async void OnGetStartedClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());

    }
}