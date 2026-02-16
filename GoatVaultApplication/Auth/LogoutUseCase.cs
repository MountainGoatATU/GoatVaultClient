using GoatVaultCore;
using GoatVaultCore.Models;

namespace GoatVaultApplication.Auth;

internal class LogoutUseCase(ISessionContext session)
{
    public Task ExecuteAsync()
    {
        session.End();
        return Task.CompletedTask;
    }
}