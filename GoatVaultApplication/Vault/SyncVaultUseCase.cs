using GoatVaultCore.Abstractions;

namespace GoatVaultApplication.Vault;

public class SyncVaultUseCase(ISyncingService syncing)
{
    public Task ExecuteAsync() => syncing.Sync();
}
