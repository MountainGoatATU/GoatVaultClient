using GoatVaultCore.Abstractions;

namespace GoatVaultApplication.Auth;

public class LogoutUseCase(ISessionContext session)
{
    public Task ExecuteAsync()
    {
        session.End();
        return Task.CompletedTask;
    }
}