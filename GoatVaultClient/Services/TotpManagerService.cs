using GoatVaultCore.Models;
using GoatVaultCore.Services;
using Microsoft.Extensions.Logging;

namespace GoatVaultClient.Services;

public class TotpManagerService(ILogger<TotpManagerService>? logger = null)
{
    private IDispatcherTimer? _timer;
    private VaultEntry? _trackedEntry;

    public void TrackEntry(VaultEntry? entry)
    {
        _trackedEntry = entry;

        if (_trackedEntry is { HasMfa: true } && !string.IsNullOrWhiteSpace(_trackedEntry.MfaSecret))
        {
            StartTimer();
            UpdateTotpCodes();  // Update immediately
        }
        else
            StopTimer();
    }

    private void StartTimer()
    {
        if (_timer != null)
            return;

        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer == null)
            return;

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) => UpdateTotpCodes();
        _timer.Start();
    }

    private void StopTimer()
    {
        if (_timer == null)
            return;

        _timer.Stop();
        _timer = null;
    }

    private void UpdateTotpCodes()
    {
        if (_trackedEntry == null || string.IsNullOrWhiteSpace(_trackedEntry.MfaSecret))
        {
            StopTimer();
            return;
        }

        try
        {
            var (code, secondsRemaining) = TotpService.GenerateCodeWithTime(_trackedEntry.MfaSecret);
            _trackedEntry.CurrentTotpCode = code;
            _trackedEntry.TotpTimeRemaining = secondsRemaining;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error generating TOTP code");
            _trackedEntry.CurrentTotpCode = "ERROR";
            _trackedEntry.TotpTimeRemaining = 0;
        }
    }

    public async Task CopyTotpCodeAsync(VaultEntry? entry)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.CurrentTotpCode))
            return;

        await Clipboard.Default.SetTextAsync(entry.CurrentTotpCode);

        // Clear clipboard after 10 seconds (fire and forget task, but handled safely)
        _ = Task.Run(async () =>
        {
            await Task.Delay(10000);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var current = await Clipboard.Default.GetTextAsync();
                if (current == entry.CurrentTotpCode)
                    await Clipboard.Default.SetTextAsync("");
            });
        });
    }
}
