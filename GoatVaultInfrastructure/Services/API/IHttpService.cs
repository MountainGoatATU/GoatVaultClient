namespace GoatVaultInfrastructure.Services.Api;

public interface IHttpService
{
    Task<T> GetAsync<T>(string url);
    Task<T> PostAsync<T>(string url, object payload);
    Task<T> PatchAsync<T>(string url, object payload);
    Task<T> DeleteAsync<T>(string url);
}