using CommunityToolkit.Mvvm.ComponentModel;

namespace GoatVaultClient.Services;

public partial class GoatTipsService : ObservableObject
{
    [ObservableProperty] private string currentTip = string.Empty;
    [ObservableProperty] private bool isTipVisible = false;

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
            counter++;

            switch (counter % 10)
            {
                // Every 10 seconds show a comment
                case 0:
                    CurrentTip = _goatTips[random.Next(_goatTips.Count)];
                    IsTipVisible = true;
                    break;
                // Disappear after 5 seconds
                case 5:
                    IsTipVisible = false;
                    CurrentTip = "";
                    break;
            }

            if (counter >= 10) counter = 0; // Reset counter
        };
        _timer.Start();
    }
}
