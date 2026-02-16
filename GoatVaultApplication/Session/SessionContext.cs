using GoatVaultCore.Models;
using GoatVaultCore.Models.Vault;

namespace GoatVaultApplication.Session;

public class SessionContext
{
    public LoggedInUser? User { get; private set; }
    public DecryptedVault? DecryptedVault { get; private set; }
    public bool IsAuthenticated { get; private set; }

    public void Start(LoggedInUser user, DecryptedVault decryptedVault)
    {
        User = user;
        DecryptedVault = decryptedVault;
        IsAuthenticated = true;
    }

    public void End()
    {
        IsAuthenticated = false;
        DecryptedVault = null;
        User = null;
    }
}