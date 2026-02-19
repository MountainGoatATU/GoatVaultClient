namespace GoatVaultCore.Abstractions;

public interface IRandomTipService
{
    string GetRandomWeakMasterPasswordTip();
    string GetRandomMediumMasterPasswordTip();
    string GetRandomPasswordReuseTip();
    string GetRandomWeakAveragePasswordTip();
    string GetRandomBreachedPasswordsTip();
    string GetRandomMfaTip();
    string GetRandomLowVaultScoreTip();
    string GetRandomMediumVaultScoreTip();
    string GetRandomHighVaultScoreTip();
}
