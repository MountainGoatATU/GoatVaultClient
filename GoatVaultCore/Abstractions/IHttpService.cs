namespace GoatVaultCore.Abstractions;

public interface IHttpService
{
    Task<T> GetAsync<T>(string url, CancellationToken ct = default);
    Task<T> PostAsync<T>(string url, object payload, CancellationToken ct = default);
    Task<T> PatchAsync<T>(string url, object payload, CancellationToken ct = default);
    Task<T> DeleteAsync<T>(string url, CancellationToken ct = default);
}
