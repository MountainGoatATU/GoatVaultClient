using CommunityToolkit.Mvvm.ComponentModel;
using GoatVaultCore.Abstractions;

namespace GoatVaultClient.Services;

public partial class GoatTipsService : ObservableObject
{
    private const string GoatEnabledKey = "GoatEnabled";

    [ObservableProperty] private string? _currentTip;
    [ObservableProperty] private bool _isTipVisible;
    [ObservableProperty] private bool _isGoatEnabled;

    private readonly Random _random = new();
    private IDispatcherTimer? _timer;

    private readonly ISessionContext _session;
    private readonly IUserRepository _users;
    private readonly IVaultScoreCalculatorService _vaultScoreCalculator;
    private readonly IRandomTipService _randomTips;

    public GoatTipsService(
        ISessionContext session,
        IUserRepository users,
        IVaultScoreCalculatorService vaultScoreCalculator,
        IRandomTipService randomTips)
    {
        _session = session;
        _users = users;
        _vaultScoreCalculator = vaultScoreCalculator;
        IsGoatEnabled = Preferences.Default.Get(GoatEnabledKey, true);
        _randomTips = randomTips;
    }

    public void SetEnabled(bool enabled)
    {
        if (IsGoatEnabled == enabled)
            return;

        IsGoatEnabled = enabled;
        Preferences.Default.Set(GoatEnabledKey, enabled);

        if (enabled)
            return;

        IsTipVisible = false;
        CurrentTip = string.Empty;
    }

    public void ApplyEnabledState(bool enabled) => SetEnabled(enabled);

    public async Task StartTips()
    {
        // Prevent multiple timers from being created
        if (_timer != null)
            return;

        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer == null)
            return;

        // Counts 5-second ticks. Used to control the 60-second display cycle.
        var tick = 0;

        // Timer fires every 5 seconds.
        _timer.Interval = TimeSpan.FromSeconds(5);

        // Prevent overlapping async executions if a tick takes longer than 5 seconds.
        var isRunning = false;

        _timer.Tick += async (_, _) =>
        {
            // Skip this tick if previous execution hasn't finished.
            if (isRunning)
                return;

            isRunning = true;

            try
            {
                // If tips are disabled or no user session exists, ensure no tip is displayed.
                if (!IsGoatEnabled || _session.UserId == null)
                {
                    HideTip();
                    return;
                }

                // Advance the cycle counter (1 tick = 5 seconds)
                tick++;

                // 12 ticks = 60 seconds total cycle
                switch (tick % 12)
                {
                    // Beginning of cycle → show new contextual tip
                    case 0:
                        await ShowTip();
                        break;
                    // One tick later (5 seconds) → hide tip
                    case 1:
                        HideTip();
                        break;
                }
            }
            finally
            {
                isRunning = false;
            }
        };

        _timer.Start();
    }

    private async Task ShowTip()
    {
        CurrentTip = await GetContextualTip();
        IsTipVisible = !string.IsNullOrWhiteSpace(CurrentTip);
    }

    private void HideTip()
    {
        CurrentTip = string.Empty;
        IsTipVisible = false;
    }

    private async Task<string> GetContextualTip()
    {
        var userId = _session.UserId;
        if (userId == null)
            return string.Empty;

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return string.Empty;
        var entries = _session.Vault?.Entries ?? [];
        var masterStrength = _session.MasterPasswordStrength;

        var score = _vaultScoreCalculator.CalculateScore(
            entries,
            masterStrength,
            user.MfaEnabled);

        // Build list of all current problems
        var problems = new List<Func<string>>();

        switch (score.MasterPasswordPercent)
        {
            case < 40:
                problems.Add(_randomTips.GetRandomWeakMasterPasswordTip);
                break;
            case < 70:
                problems.Add(_randomTips.GetRandomMediumMasterPasswordTip);
                break;
        }

        if (score.ReuseRatePercent < 70)
            problems.Add(_randomTips.GetRandomPasswordReuseTip);

        if (score.AveragePasswordsPercent < 60)
            problems.Add(_randomTips.GetRandomWeakAveragePasswordTip);

        if (score.BreachesCount > 0)
            problems.Add(_randomTips.GetRandomBreachedPasswordsTip);

        if (!score.MfaEnabled)
            problems.Add(_randomTips.GetRandomMfaTip);

        switch (score.VaultScore)
        {
            case < 400:
                problems.Add(_randomTips.GetRandomLowVaultScoreTip);
                break;
            case < 700:
                problems.Add(_randomTips.GetRandomMediumVaultScoreTip);
                break;
        }

        // If no problems detected, use high‑score motivational tips
        if (problems.Count == 0)
            return _randomTips.GetRandomHighVaultScoreTip();

        // Randomly pick one of the active problem categories
        var picker = problems[_random.Next(problems.Count)];
        return picker();
    }
}
