using GoatVaultClient_v2.Models;
using GoatVaultClient_v2.Services;

namespace GoatVaultClient_v2
{
    public partial class MainPage : ContentPage
    {
        private readonly HttpService _httpService;
        private readonly VaultService _vaultService;

        public MainPage(HttpService httpService, VaultService vaultService)
        {
            InitializeComponent();
            _httpService = httpService;
            _vaultService = vaultService;
        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            string email = "example4579@gmail.com"; // Must be unique for each run
            string password = "password";
            string userId = "b1c1f27a-cc59-4d2b-ae74-7b3b0e33a61a";
            string vaultId = "adef17a9-ab47-4c27-832c-75ed590ac663";

            // 1. Retrieve all vaults for specified user
            string userVaultsUrl = $"http://127.0.0.1:8000/v1/users/{userId}/vaults/";
            var userVaults = await _httpService.GetAsync<VaultListResponse>(userVaultsUrl);

            // 2. Create local vault
            var vaultPayload = _vaultService.EncryptVault(password);
            await _vaultService.SaveVaultToLocalAsync(vaultPayload);

            // 3. If vault exists on server, compare nonce, if different, upload local vault to server + if vaults does not exists on server, upload local vault to server
            var serverVault = userVaults.Vaults.FirstOrDefault(v => v.Id == vaultPayload.Id);

            if (serverVault != null)
            {
                // Vault exists on server, compare nonce
                if (serverVault.Nonce != vaultPayload.Nonce)
                {
                    // Nonces are different, update server vault with local vault
                    string vaultUrl = $"http://127.0.0.1:8000/v1/users/{userId}/vaults/";
                    var vaultResponse = await _httpService.PatchAsync<VaultPayload>(vaultUrl, vaultPayload);

                }
                // Nonces are the same, no action needed
            }
            else
            {
                // Vault does not exist on server, upload local vault to server
                string vaultUrl = $"http://127.0.0.1:8000/v1/users/{userId}/vaults/";
                var vaultResponse = await _httpService.PostAsync<VaultPayload>(vaultUrl, vaultPayload);
            }
        }
    }

}
