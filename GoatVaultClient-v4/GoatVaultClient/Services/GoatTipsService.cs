using CommunityToolkit.Mvvm.ComponentModel;

namespace GoatVaultClient.Services;

public partial class GoatTipsService : ObservableObject
{
    private const string GoatEnabledKey = "GoatEnabled";

    [ObservableProperty] private string currentTip = string.Empty;
    [ObservableProperty] private bool isTipVisible;
    [ObservableProperty] private bool isGoatEnabled;

    private readonly List<string> _goatTips =
    [
        "Tip: Change your password regularly!",
        "Tip: Avoid using the same password twice.",
        "Tip: Enable two-factor authentication.",
        "Tip: Your Vault score is low, check weak passwords.",
        "Tip: Keep backup keys handy.",
        "Tip: Consider using a passphrase instead of a single word."
    ];

    private readonly Random _random = new();
    private IDispatcherTimer? _timer;

    public GoatTipsService()
    {
        // Read persisted state from the device storage via MAUI Preferences
        IsGoatEnabled = Preferences.Default.Get(GoatEnabledKey, true);
    }

    public void SetEnabled(bool enabled)
    {
        if (IsGoatEnabled == enabled)
            return;

        IsGoatEnabled = enabled;

        // Persist state to device storage via MAUI Preferences.
        Preferences.Default.Set(GoatEnabledKey, enabled);

        if (!enabled)
        {
            IsTipVisible = false;
            CurrentTip = string.Empty;
        }
    }

    public void ApplyEnabledState(bool enabled) => SetEnabled(enabled);

    public void StartTips()
    {
        if (_timer != null)
            return;

        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer == null)
            return;

        var counter = 0;
        _timer.Interval = TimeSpan.FromSeconds(1);

        _timer.Tick += (_, _) =>
        {
            if (!IsGoatEnabled)
            {
                IsTipVisible = false;
                CurrentTip = string.Empty;
                return;
            }

            counter++;

            switch (counter % 10)
            {
                case 0:
                    CurrentTip = _goatTips[_random.Next(_goatTips.Count)];
                    IsTipVisible = true;
                    break;

                case 5:
                    IsTipVisible = false;
                    CurrentTip = string.Empty;
                    break;
            }

            if (counter >= 10)
                counter = 0;
        };

        _timer.Start();
    }
}
