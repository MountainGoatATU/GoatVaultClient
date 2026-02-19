using GoatVaultCore.Abstractions;

namespace GoatVaultApplication.Auth;

public class LogoutUseCase(ISessionContext session, ISyncingService syncing)
{
    public Task ExecuteAsync()
    {
        syncing.StopPeriodicSync();
        session.End();
        return Task.CompletedTask;
    }
}