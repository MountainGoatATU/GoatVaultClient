using GoatVaultCore.Abstractions;

namespace GoatVaultApplication.VaultUseCases;

public class SyncVaultUseCase(ISyncingService syncing)
{
    public Task ExecuteAsync() => syncing.Sync();
}
