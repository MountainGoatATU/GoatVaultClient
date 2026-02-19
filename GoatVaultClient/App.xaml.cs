using GoatVaultCore.Abstractions;
using Microsoft.Extensions.Logging;

namespace GoatVaultClient;

public partial class App : Application
{
    private readonly ISessionContext _session;
    private readonly IUserRepository _users;
    private readonly ILogger<App> _logger;

    public App(ISessionContext session, IUserRepository users, ILogger<App> logger)
    {
        InitializeComponent();
        MainPage = new AppShell();
        _session = session;
        _users = users;
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

            if (_session.Vault == null || _session.UserId == null)
            {
                _logger.LogDebug("No active vault session to save on stop");
                return;
            }

            // TODO: Fix
            // Use synchronous save - we're in a non-async event handler
            /*
            var saveTask = _users.SaveVaultAsync(
                _session.CurrentUser,
                _session.MasterPassword,
                _session.DecryptedVault);

            // Wait with timeout
            if (saveTask.Wait(TimeSpan.FromSeconds(3)))
            {
                _logger.LogInformation("Vault saved successfully on stop");
            }
            else
            {
                _logger.LogWarning("Vault save timed out on stop (3s)");
            }
            */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving vault on stop");
        }
    }

    protected override async void OnStart()
    {
        base.OnStart();

        // Retrieve the "IsFirstRun" flag. If it doesn't exist, it defaults to true.
        bool isFirstRun = Preferences.Default.Get("IsFirstRun", true);

        if (isFirstRun)
        {
            // Set the flag to false so this block never runs again
            Preferences.Default.Set("IsFirstRun", false);

            // Route to the Introduction / Onboarding flow
            await Shell.Current.GoToAsync("//intro");
        }
        else
        {
            // Route directly to the Login page for returning users
            await Shell.Current.GoToAsync("//login");
        }
    }

    protected override void OnSleep()
    {
        // Save when app goes to background
        try
        {
            _logger.LogInformation("App going to sleep — saving vault");

            if (_session.Vault != null && _session.UserId != null)
            {
                // TODO: Fix
                /*
                var saveTask = _users.SaveVaultAsync(
                    _session.CurrentUser,
                    _session.MasterPassword,
                    _session.DecryptedVault);

                saveTask.Wait(TimeSpan.FromSeconds(3));
                */
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

        // TODO: Fix
        // _session.Lock();
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
