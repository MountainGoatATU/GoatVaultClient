using CommunityToolkit.Mvvm.ComponentModel;

namespace GoatVaultClient.Services;

public partial class GoatTipsService : ObservableObject
{
    [ObservableProperty] private string currentTip = string.Empty;
    [ObservableProperty] private bool isTipVisible;
    [ObservableProperty] private bool isGoatEnabled = true;

    private readonly List<string> _goatTips =
    [
        "Tip: Change your password regularly!",
        "Tip: Avoid using the same password twice.",
        "Tip: Enable two-factor authentication.",
        "Tip: Your Vault score is low, check weak passwords.",
        "Tip: Keep backup keys handy.",
        "Tip: Consider using a passphrase instead of a single word."
    ];

    private IDispatcherTimer? _timer;

    public void ApplyEnabledState(bool enabled)
    {
        var current = Preferences.Default.Get("GoatEnabled", true);
        if (current != enabled)
            Preferences.Default.Set("GoatEnabled", enabled);

        IsGoatEnabled = enabled;

        if (!enabled)
        {
            IsTipVisible = false;
            CurrentTip = string.Empty;
        }
    }

    public void StartTips()
    {
        if (_timer != null) return;

        var random = new Random();
        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer == null) return;

        var counter = 0;
        _timer.Interval = TimeSpan.FromSeconds(1);

        _timer.Tick += (s, e) =>
        {
            var goatEnabled = Preferences.Default.Get("GoatEnabled", true);

            // keep IsGoatEnabled in sync even if changed elsewhere
            if (IsGoatEnabled != goatEnabled)
                IsGoatEnabled = goatEnabled;

            if (!goatEnabled)
            {
                IsTipVisible = false;
                CurrentTip = string.Empty;
                return;
            }

            counter++;

            switch (counter % 10)
            {
                case 0:
                    CurrentTip = _goatTips[random.Next(_goatTips.Count)];
                    IsTipVisible = true;
                    break;
                case 5:
                    IsTipVisible = false;
                    CurrentTip = string.Empty;
                    break;
            }

            if (counter >= 10) counter = 0;
        };

        _timer.Start();
    }
}
