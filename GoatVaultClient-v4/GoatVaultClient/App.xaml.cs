using GoatVaultInfrastructure.Services.Vault;

namespace GoatVaultClient;

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
        window.Stopped += Window_Stopped;
        return window;
    }

    private void Window_Stopped(object? sender, EventArgs e)
    {
        // This event fires before service disposal
        try
        {
            System.Diagnostics.Debug.WriteLine("App stopped - attempting to save vault...");

            if (_sessionService.DecryptedVault == null ||
                string.IsNullOrEmpty(_sessionService.MasterPassword) ||
                _sessionService.CurrentUser == null)
            {
                return;
            }

            // Use synchronous save - we're in a non-async event handler
            var saveTask = _vaultService.SaveVaultAsync(
                _sessionService.CurrentUser,
                _sessionService.MasterPassword,
                _sessionService.DecryptedVault);

            // Wait with timeout
            System.Diagnostics.Debug.WriteLine(saveTask.Wait(TimeSpan.FromSeconds(3))
                ? "Vault saved successfully on stop"
                : "Vault save timed out");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving vault on stop: {ex}");
        }
    }

    protected override void OnSleep()
    {
        // Save when app goes to background
        try
        {
            System.Diagnostics.Debug.WriteLine("App going to sleep - saving vault...");

            if (_sessionService.DecryptedVault != null &&
                !string.IsNullOrEmpty(_sessionService.MasterPassword) &&
                _sessionService.CurrentUser != null)
            {
                var saveTask = _vaultService.SaveVaultAsync(
                    _sessionService.CurrentUser,
                    _sessionService.MasterPassword,
                    _sessionService.DecryptedVault);

                saveTask.Wait(TimeSpan.FromSeconds(3));
                System.Diagnostics.Debug.WriteLine("Vault saved on sleep");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving on sleep: {ex}");
        }

        _sessionService.Lock();
    }

    // protected override Window CreateWindow(IActivationState? activationState)
    // {
    //     var introductionPage = _services.GetRequiredService<IntroductionPage>();
    //     return new Window(new NavigationPage(introductionPage));
    // }
}