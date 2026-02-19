using CommunityToolkit.Mvvm.ComponentModel;
using GoatVaultCore.Abstractions;

namespace GoatVaultClient.Services;

public partial class GoatTipsService : ObservableObject
{
    private const string GoatEnabledKey = "GoatEnabled";

    [ObservableProperty] private string? currentTip;
    [ObservableProperty] private bool isTipVisible;
    [ObservableProperty] private bool isGoatEnabled;

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
        if (_timer != null)
            return;

        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer == null)
            return;

        var counter = 0;
        _timer.Interval = TimeSpan.FromSeconds(1);

        _timer.Tick += async (_, _) =>
        {
            if (!IsGoatEnabled || _session.UserId == null)
            {
                IsTipVisible = false;
                CurrentTip = string.Empty;
                return;
            }

            counter++;

            switch (counter % 10)
            {
                case 0:
                    CurrentTip = await GetContextualTip();
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

    private async Task<string> GetContextualTip()
    {
        var userId = _session.UserId ?? throw new InvalidOperationException("UserId is null");
        var user = await _users.GetByIdAsync(userId) ?? throw new InvalidOperationException("User is null");
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