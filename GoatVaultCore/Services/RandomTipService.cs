using GoatVaultCore.Abstractions;

namespace GoatVaultCore.Services;

public class RandomTipService : IRandomTipService
{
    private readonly Random _random = new();

    #region Tips

    private readonly string[] _weakMasterPasswordTips =
    [
        "Your master password is very weak. Aim for at least 12–16 random characters.",
        "Weak master password detected. Use a long passphrase of 4–6 random words.",
        "Avoid common words and patterns in your master password. Attackers guess those first.",
        "If you can easily remember your master password, try making it longer and more random.",
        "Never reuse your master password on any other site or service.",
        "Consider using a passphrase like \"correct horse battery staple\" style – long and random.",
        "Your vault strength is held back by a weak master password. Strengthen it to climb the score.",
        "Update your master password before anything else – it protects everything in your vault."
    ];

    private readonly string[] _mediumMasterPasswordTips =
    [
        "Your master password is decent, but you can still improve it by adding length.",
        "Add a few more random characters to your master password for extra safety.",
        "Avoid patterns like seasons+year (e.g. Spring2024) in your master password.",
        "Mix upper, lower, digits, and symbols in your master password for more entropy.",
        "Consider switching to a long passphrase to push your master password strength higher.",
        "A slightly stronger master password could give your vault score a noticeable boost."
    ];

    private readonly string[] _passwordReuseTips =
    [
        "You reuse many passwords. Start making each site’s password unique.",
        "Begin by fixing reuse on email and banking accounts – they’re the most critical.",
        "Reused passwords mean one breach can unlock many accounts. Break that chain.",
        "Use the generator to create fresh passwords instead of copy/pasting old ones.",
        "Sort your vault by sites using the same password and change them one by one.",
        "Even small changes help: pick 3 reused passwords today and replace them.",
        "Think of each website as a separate lock – it deserves its own unique key.",
        "Low originality detected. Unique passwords are the best defense against credential stuffing."
    ];

    private readonly string[] _weakAveragePasswordTips =
    [
        "Many of your passwords are weak. Aim for at least 14 characters for important accounts.",
        "Upgrade short passwords to longer, randomly generated ones.",
        "Avoid passwords based on names, dates, or dictionary words – they’re easy to guess.",
        "Start by strengthening passwords for email, banking, and social media.",
        "Simple substitutions like 'a'→'@' or 'o'→'0' are not enough anymore.",
        "Use the vault’s generator – humans are bad at inventing strong random passwords.",
        "Any password you can type without looking may be too simple. Consider a stronger one.",
        "Weak passwords drag your score down. A few upgrades can make a big difference."
    ];

    private readonly string[] _breachedPasswordTips =
    [
        "Some passwords appear in breaches. Change them immediately on the affected sites.",
        "Never reuse a breached password, even on a completely new website.",
        "After changing a breached password, also enable MFA on that account if possible.",
        "If the breached password was used in multiple places, change it everywhere.",
        "Check inbox filters and forwarding if your email account was involved in a breach.",
        "Treat breached passwords as permanently burned secrets – never resurrect them.",
        "Use unique, random passwords so a single breach can’t spread to other accounts.",
        "Leaked passwords spread quickly. Rotate any breached ones as soon as you can."
    ];

    private readonly string[] _mfaTips =
    [
        "Enable MFA on your account to add a strong second layer of protection.",
        "MFA can stop many attacks even if someone guesses your password.",
        "Start by enabling MFA on your email account – it unlocks lots of other services.",
        "Prefer authenticator apps over SMS codes when you can.",
        "Combine a strong password with MFA for the best protection.",
        "Think of MFA as a seatbelt for your account – you miss it only when it’s too late.",
        "If a service offers MFA, it’s usually because attackers target it often. Turn it on.",
        "Turning on MFA is one of the fastest ways to improve your security posture."
    ];

    private readonly string[] _lowVaultScoreTips =
    [
        "Your overall vault score is low. Start with the master password and MFA.",
        "You’re at the beginning of your security journey. Fix one weak spot at a time.",
        "Focus first on the accounts that matter most: email, banking, cloud storage.",
        "Improving just a handful of critical passwords can noticeably raise your score.",
        "Don’t worry about perfection – consistent small improvements are what matter.",
        "You have lots of room to grow. Start with the biggest risks and move upward."
    ];

    private readonly string[] _mediumVaultScoreTips =
    [
        "You’re halfway up the mountain. Clean up remaining weak or reused passwords.",
        "Nice progress. Review old accounts you don’t use and close or update them.",
        "Look for any sites still sharing the same password and break those clusters.",
        "To push your score higher, raise all weak passwords to at least medium strength.",
        "Check that your most sensitive accounts also have MFA turned on.",
        "You’re doing well. A bit more work will move you into the top security tier."
    ];

    private readonly string[] _highVaultScoreTips =
    [
        "Great job! Your vault looks strong. Keep rotating important passwords from time to time.",
        "You’re near the summit. Consider adding MFA wherever it’s still missing.",
        "Periodically audit your vault for unused or risky services and clean them up.",
        "Store recovery codes for MFA somewhere safe and offline.",
        "For critical accounts, think about hardware security keys where supported.",
        "Security is a habit. Schedule a quick monthly review of your vault.",
        "Your setup is solid. Now help others around you improve their password habits too.",
        "Staying secure is an ongoing process, and you’re doing it right. Keep going."
    ];

    #endregion

    private string GetRandomTip(string[] tips) => tips[_random.Next(tips.Length)];

    public string GetRandomWeakMasterPasswordTip() => GetRandomTip(_weakMasterPasswordTips);
    public string GetRandomMediumMasterPasswordTip() => GetRandomTip(_mediumMasterPasswordTips);
    public string GetRandomPasswordReuseTip() => GetRandomTip(_passwordReuseTips);
    public string GetRandomWeakAveragePasswordTip() => GetRandomTip(_weakAveragePasswordTips);
    public string GetRandomBreachedPasswordsTip() => GetRandomTip(_breachedPasswordTips);
    public string GetRandomMfaTip() => GetRandomTip(_mfaTips);
    public string GetRandomLowVaultScoreTip() => GetRandomTip(_lowVaultScoreTips);
    public string GetRandomMediumVaultScoreTip() => GetRandomTip(_mediumVaultScoreTips);
    public string GetRandomHighVaultScoreTip() => GetRandomTip(_highVaultScoreTips);
}
