using GoatVaultClient.Pages;

namespace GoatVaultClient;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(GratitudePage), typeof(GratitudePage));
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(IntroductionPage), typeof(IntroductionPage));
        Routing.RegisterRoute(nameof(EducationPage), typeof(EducationPage));
        Routing.RegisterRoute(nameof(EducationDetailPage), typeof(EducationDetailPage));
        Routing.RegisterRoute(nameof(UserPage), typeof(UserPage));
    }
}