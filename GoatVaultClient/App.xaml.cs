using GoatVaultInfrastructure.Services.Vault;
using Microsoft.Extensions.Logging;

namespace GoatVaultClient;

public partial class App : Application
{
    private readonly VaultSessionService _sessionService;
    private readonly VaultService _vaultService;
    private readonly ILogger<App> _logger;

    public App(VaultSessionService sessionService, VaultService vaultService, ILogger<App> logger)
    {
        InitializeComponent();
        MainPage = new AppShell();
        _sessionService = sessionService;
        _vaultService = vaultService;
        _logger = logger;

        // Wire up global exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _logger.LogInformation("Application started");
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Stopped += Window_Stopped;
        window.Resumed += (_, _) => _logger.LogInformation("Application resumed");
        return window;
    }

    private void Window_Stopped(object? sender, EventArgs e)
    {
        // This event fires before service disposal
        try
        {
            _logger.LogInformation("App stopped — attempting to save vault");

            if (_sessionService.DecryptedVault == null ||
                string.IsNullOrEmpty(_sessionService.MasterPassword) ||
                _sessionService.CurrentUser == null)
            {
                _logger.LogDebug("No active vault session to save on stop");
                return;
            }

            // Use synchronous save - we're in a non-async event handler
            var saveTask = _vaultService.SaveVaultAsync(
                _sessionService.CurrentUser,
                _sessionService.MasterPassword,
                _sessionService.DecryptedVault);

            // Wait with timeout
            if (saveTask.Wait(TimeSpan.FromSeconds(3)))
            {
                _logger.LogInformation("Vault saved successfully on stop");
            }
            else
            {
                _logger.LogWarning("Vault save timed out on stop (3s)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving vault on stop");
        }
    }

    protected override void OnSleep()
    {
        // Save when app goes to background
        try
        {
            _logger.LogInformation("App going to sleep — saving vault");

            if (_sessionService.DecryptedVault != null &&
                !string.IsNullOrEmpty(_sessionService.MasterPassword) &&
                _sessionService.CurrentUser != null)
            {
                var saveTask = _vaultService.SaveVaultAsync(
                    _sessionService.CurrentUser,
                    _sessionService.MasterPassword,
                    _sessionService.DecryptedVault);

                saveTask.Wait(TimeSpan.FromSeconds(3));
                _logger.LogInformation("Vault saved on sleep");
            }
            else
            {
                _logger.LogDebug("No active vault session to save on sleep");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving vault on sleep");
        }

        _sessionService.Lock();
    }

    #region Global Exception Handlers

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _logger.LogCritical(ex, "Unhandled AppDomain exception (IsTerminating: {IsTerminating})", e.IsTerminating);
        }
        else
        {
            _logger.LogCritical("Unhandled AppDomain exception of type {Type} (IsTerminating: {IsTerminating})",
                e.ExceptionObject?.GetType().Name ?? "unknown", e.IsTerminating);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "Unobserved task exception");
        e.SetObserved(); // Prevent app crash
    }

    #endregion
}
