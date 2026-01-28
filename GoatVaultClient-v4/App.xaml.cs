using GoatVaultClient_v4.Services.Vault;

namespace GoatVaultClient_v4;

public partial class App : Application
{
    private readonly VaultSessionService _sessionService;
    private readonly VaultService _vaultService;

    public App(VaultSessionService sessionService, VaultService vaultService)
    {
        InitializeComponent();
        MainPage = new AppShell();
        _sessionService = sessionService;
        _vaultService = vaultService;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Destroying += Window_Destroying;
        return window;
    }

    private async void Window_Destroying(object sender, EventArgs e)
    {
        if (_sessionService != null && _vaultService != null && _sessionService.DecryptedVault != null && !string.IsNullOrEmpty(_sessionService.MasterPassword))
        {
            await _vaultService.SyncAndCloseAsync(_sessionService.CurrentUser, _sessionService.MasterPassword, _sessionService.DecryptedVault);
        }
    }

    protected override void OnSleep() => _sessionService.Lock();

    // protected override Window CreateWindow(IActivationState? activationState)
    // {
    //     var introductionPage = _services.GetRequiredService<IntroductionPage>();
    //     return new Window(new NavigationPage(introductionPage));
    // }
}