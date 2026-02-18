using CommunityToolkit.Mvvm.ComponentModel;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Services;

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
    private readonly VaultScoreCalculatorService _vaultScoreCalculator;

    public GoatTipsService(
        ISessionContext session,
        IUserRepository users,
        VaultScoreCalculatorService vaultScoreCalculator)
    {
        _session = session;
        _users = users;
        _vaultScoreCalculator = vaultScoreCalculator;
        IsGoatEnabled = Preferences.Default.Get(GoatEnabledKey, true);
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
                problems.Add(GetVeryWeakMasterTip);
                break;
            case < 70:
                problems.Add(GetMediumMasterTip);
                break;
        }

        if (score.ReuseRatePercent < 70)
        {
            problems.Add(GetReuseTip);
        }

        if (score.AveragePasswordsPercent < 60)
        {
            problems.Add(GetWeakAverageTip);
        }

        if (score.BreachesCount > 0)
        {
            problems.Add(GetBreachTip);
        }

        if (!score.MfaEnabled)
        {
            problems.Add(GetMfaTip);
        }

        switch (score.VaultScore)
        {
            case < 400:
                problems.Add(GetLowScoreTip);
                break;
            case < 700:
                problems.Add(GetMidScoreTip);
                break;
        }

        // If no problems detected, use high‑score motivational tips
        if (problems.Count == 0)
        {
            return GetHighScoreTip();
        }

        // Randomly pick one of the active problem categories
        var picker = problems[_random.Next(problems.Count)];
        return picker();


        string GetVeryWeakMasterTip() =>
            _random.Next(8) switch
            {
                0 => "Your master password is very weak. Aim for at least 12–16 random characters.",
                1 => "Weak master password detected. Use a long passphrase of 4–6 random words.",
                2 => "Avoid common words and patterns in your master password. Attackers guess those first.",
                3 => "If you can easily remember your master password, try making it longer and more random.",
                4 => "Never reuse your master password on any other site or service.",
                5 => "Consider using a passphrase like \"correct horse battery staple\" style – long and random.",
                6 => "Your vault strength is held back by a weak master password. Strengthen it to climb the score.",
                _ => "Update your master password before anything else – it protects everything in your vault."
            };

        string GetMediumMasterTip() =>
            _random.Next(6) switch
            {
                0 => "Your master password is decent, but you can still improve it by adding length.",
                1 => "Add a few more random characters to your master password for extra safety.",
                2 => "Avoid patterns like seasons+year (e.g. Spring2024) in your master password.",
                3 => "Mix upper, lower, digits, and symbols in your master password for more entropy.",
                4 => "Consider switching to a long passphrase to push your master password strength higher.",
                _ => "A slightly stronger master password could give your vault score a noticeable boost."
            };

        string GetReuseTip() =>
            _random.Next(8) switch
            {
                0 => "You reuse many passwords. Start making each site’s password unique.",
                1 => "Begin by fixing reuse on email and banking accounts – they’re the most critical.",
                2 => "Reused passwords mean one breach can unlock many accounts. Break that chain.",
                3 => "Use the generator to create fresh passwords instead of copy/pasting old ones.",
                4 => "Sort your vault by sites using the same password and change them one by one.",
                5 => "Even small changes help: pick 3 reused passwords today and replace them.",
                6 => "Think of each website as a separate lock – it deserves its own unique key.",
                _ => "Low originality detected. Unique passwords are the best defense against credential stuffing."
            };

        string GetWeakAverageTip() =>
            _random.Next(8) switch
            {
                0 => "Many of your passwords are weak. Aim for at least 14 characters for important accounts.",
                1 => "Upgrade short passwords to longer, randomly generated ones.",
                2 => "Avoid passwords based on names, dates, or dictionary words – they’re easy to guess.",
                3 => "Start by strengthening passwords for email, banking, and social media.",
                4 => "Simple substitutions like 'a'→'@' or 'o'→'0' are not enough anymore.",
                5 => "Use the vault’s generator – humans are bad at inventing strong random passwords.",
                6 => "Any password you can type without looking may be too simple. Consider a stronger one.",
                _ => "Weak passwords drag your score down. A few upgrades can make a big difference."
            };

        string GetBreachTip() =>
            _random.Next(8) switch
            {
                0 => "Some passwords appear in breaches. Change them immediately on the affected sites.",
                1 => "Never reuse a breached password, even on a completely new website.",
                2 => "After changing a breached password, also enable MFA on that account if possible.",
                3 => "If the breached password was used in multiple places, change it everywhere.",
                4 => "Check inbox filters and forwarding if your email account was involved in a breach.",
                5 => "Treat breached passwords as permanently burned secrets – never resurrect them.",
                6 => "Use unique, random passwords so a single breach can’t spread to other accounts.",
                _ => "Leaked passwords spread quickly. Rotate any breached ones as soon as you can."
            };

        string GetMfaTip() =>
            _random.Next(8) switch
            {
                0 => "Enable MFA on your account to add a strong second layer of protection.",
                1 => "MFA can stop many attacks even if someone guesses your password.",
                2 => "Start by enabling MFA on your email account – it unlocks lots of other services.",
                3 => "Prefer authenticator apps over SMS codes when you can.",
                4 => "Combine a strong password with MFA for the best protection.",
                5 => "Think of MFA as a seatbelt for your account – you miss it only when it’s too late.",
                6 => "If a service offers MFA, it’s usually because attackers target it often. Turn it on.",
                _ => "Turning on MFA is one of the fastest ways to improve your security posture."
            };

        string GetLowScoreTip() =>
            _random.Next(6) switch
            {
                0 => "Your overall vault score is low. Start with the master password and MFA.",
                1 => "You’re at the beginning of your security journey. Fix one weak spot at a time.",
                2 => "Focus first on the accounts that matter most: email, banking, cloud storage.",
                3 => "Improving just a handful of critical passwords can noticeably raise your score.",
                4 => "Don’t worry about perfection – consistent small improvements are what matter.",
                _ => "You have lots of room to grow. Start with the biggest risks and move upward."
            };

        string GetMidScoreTip() =>
            _random.Next(6) switch
            {
                0 => "You’re halfway up the mountain. Clean up remaining weak or reused passwords.",
                1 => "Nice progress. Review old accounts you don’t use and close or update them.",
                2 => "Look for any sites still sharing the same password and break those clusters.",
                3 => "To push your score higher, raise all weak passwords to at least medium strength.",
                4 => "Check that your most sensitive accounts also have MFA turned on.",
                _ => "You’re doing well. A bit more work will move you into the top security tier."
            };

        string GetHighScoreTip() =>
            _random.Next(8) switch
            {
                0 => "Great job! Your vault looks strong. Keep rotating important passwords from time to time.",
                1 => "You’re near the summit. Consider adding MFA wherever it’s still missing.",
                2 => "Periodically audit your vault for unused or risky services and clean them up.",
                3 => "Store recovery codes for MFA somewhere safe and offline.",
                4 => "For critical accounts, think about hardware security keys where supported.",
                5 => "Security is a habit. Schedule a quick monthly review of your vault.",
                6 => "Your setup is solid. Now help others around you improve their password habits too.",
                _ => "Staying secure is an ongoing process, and you’re doing it right. Keep going."
            };
    }
}
