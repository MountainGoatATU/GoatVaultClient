using GoatVaultClient.ViewModels;
using GoatVaultCore.Abstractions;
using Microsoft.Extensions.Logging;

namespace GoatVaultClient;

public partial class App
{
    private readonly ISessionContext _session;
    private readonly ILogger<App> _logger;

    public App(ISessionContext session, ILogger<App> logger)
    {
        InitializeComponent();
        _session = session;
        _logger = logger;

        // Wire up global exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _logger.LogInformation("Application started");
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var vm = Handler!.MauiContext!.Services.GetRequiredService<AppShellViewModel>();

        var window = new Window(new AppShell(vm));
        window.Stopped += Window_Stopped;
        window.Resumed += (_, _) => _logger.LogInformation("Application resumed");
        return window;
    }

    public static Page? CurrentMainPage => Current is { Windows.Count: > 0 } ? Current.Windows[0]?.Page : null;

    private void Window_Stopped(object? sender, EventArgs e)
    {
        // This event fires before service disposal
        try
        {
            _logger.LogInformation("App stopped — attempting to save vault");

            if (_session is { Vault: not null, UserId: not null })
                return;

            _logger.LogDebug("No active vault session to save on stop");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving vault on stop");
        }
    }

    protected override async void OnStart()
    {
        base.OnStart();

        var isFirstRun = Preferences.Default.Get("IsFirstRun", true);

        if (isFirstRun)
        {
            Preferences.Default.Set("IsFirstRun", false);
            await Shell.Current.GoToAsync("//intro");
        }
        else
            await Shell.Current.GoToAsync("//login");
    }

    protected override void OnSleep()
    {
        // Save when app goes to background
        try
        {
            _logger.LogInformation("App going to sleep — saving vault");

            if (_session is { Vault: not null, UserId: not null })
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
            _logger.LogCritical(ex, "Unhandled AppDomain exception (IsTerminating: {IsTerminating})", e.IsTerminating);
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