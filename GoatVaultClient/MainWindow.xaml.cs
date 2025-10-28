using GoatVaultClient.Helpers;
using GoatVaultClient.Models;
using Isopoh.Cryptography.Argon2;
using Isopoh.Cryptography.SecureArray;
using Sodium;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace GoatVaultClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly HttpService _httpService = new HttpService();
        private readonly VaultService _vaultService = new VaultService();
        private readonly UserService _userService = new UserService();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string email = "example4578@gmail.com"; // Must be unique for each run
            string password = "password";
            string userId = "b1c1f27a-cc59-4d2b-ae74-7b3b0e33a61a";
            string vaultId = "9c3ea03d-6d90-4b33-8eea-b458be83839a";


            // Register user
            string registerJson = _userService.RegisterUser(email, password);
            string registerUrl = "http://127.0.0.1:8000/v1/users";
            string registerResponse = await _httpService.HttpPost(registerUrl, registerJson);
            Console.WriteLine("Registration response:");
            Console.WriteLine(registerResponse + "\n");


            // Login user
            var registeredUser = JsonSerializer.Deserialize<UserPayload>(registerJson);
            string loginResult = _userService.LoginUser(email, password, registeredUser.salt, registeredUser.password_hash);
            Console.WriteLine("Login result: " + loginResult + "\n");

            // Create vault
            string vaultPayload = _vaultService.CreateVault(password);
            string vaultUrl = $"http://127.0.0.1:8000/v1/users/{userId}/vaults/{vaultId}";
            string vaultResponse = await _httpService.HttpPost(vaultUrl, vaultPayload);
            Console.WriteLine("Vault creation response:");
            Console.WriteLine(vaultResponse + "\n");

            // Retrieve and decrypt vault
            string retrievedVaultJson = await _httpService.HttpGet(vaultUrl);
            _vaultService.DecryptVaultFromServer(retrievedVaultJson, password);
        }

        /*private void VaultTesting() 
        {
            var password = "password1";
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password); // get bytes from password
            // password1 -> produces [112, 97, 115, 115, 119, 111, 114, 100, 49]
            // p is in decimal 112 based on ASCII table
            byte[] salt = new byte[16];

            Rng.GetBytes(salt); // we create number 16 random bytes(numbers) for salt
            

            var config = new Argon2Config
            {
                Type = Argon2Type.DataIndependentAddressing,
                Version = Argon2Version.Nineteen,

                // Lineary increases CPU time required to compute the hash7
                // TODO: maybe later adjust this prop
                TimeCost = 10, // Number of iterations to perform (how many times the internal memory is processed)

                MemoryCost = 32768, // Amount of memory (in kibibytes) to use
                Lanes = 5, // Degree of parallelism (number of threads and computational lanes)
                Threads = Environment.ProcessorCount, // higher than "Lanes" doesn't help (or hurt)
                Password = passwordBytes,
                Salt = salt, // >= 8 bytes if not null
                //Secret = secret, // from somewhere
                //AssociatedData = associatedData, // from somewhere
                HashLength = 32 // bytes
            };
            var argon2A = new Argon2(config);

            /*string hashString;
            using (SecureArray<byte> hashA = argon2A.Hash())
            // hashA is the derived key
            // hashString is for account validation
            {
                hashString = config.EncodeString(hashA.Buffer); 
            }

            if (Argon2.Verify(hashString, passwordBytes, 5))
            {
                // verified
                Console.WriteLine("Verified");
            }
        byte[] derivedKey;

            using (SecureArray<byte> hashA = argon2A.Hash())
            {
                derivedKey = (byte[])hashA.Buffer.Clone(); // Clone the buffer to a regular byte array
            }

            var vaultData = new
            {
                user_id = new Guid().ToString(),
                salt = Convert.ToBase64String(salt),
            };

            string vaultJson = JsonSerializer.Serialize(vaultData);
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(vaultJson);

            byte[] nonce = new byte[12];
            Rng.GetBytes(nonce); // create random nonce

            byte[] nonce = SecretAeadXChaCha20Poly1305.GenerateNonce(); // 24 bytes

            // Encrypt the vault data using AES-GCM
            //byte[] ciphertext = new byte[plaintextBytes.Length];
            // ciphertext will hold the encrypted data
            // thats why its the same length as plaintextBytes

            // ciphertext using ChaCha20-Poly1305
            var ciphertext = SecretAeadXChaCha20Poly1305.Encrypt(plaintextBytes, nonce, derivedKey);
            /*byte[] authTag = new byte[16];

            using (var aes = new AesGcm(derivedKey, 16))
            {
                aes.Encrypt(nonce, plaintextBytes, ciphertext, authTag);
            }
            

            var payload = new
            {
                user_id = vaultData.user_id,
                salt = Convert.ToBase64String(salt),
                nonce = Convert.ToBase64String(nonce),
                auth_tag = Convert.ToBase64String(authTag),
                encrypted_blob = Convert.ToBase64String(ciphertext)
            };

            var payload = new
            {
                user_id = vaultData.user_id,
                salt = Convert.ToBase64String(salt),
                nonce = Convert.ToBase64String(nonce),
                encrypted_blob = Convert.ToBase64String(ciphertext)
            }; // auth tag is included in ciphertext for ChaCha20-Poly1305


            string payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

            byte[] decrypted = SecretAeadXChaCha20Poly1305.Decrypt(ciphertext, nonce, derivedKey);
            string decryptedText = Encoding.UTF8.GetString(decrypted);
            Console.WriteLine(decryptedText);
        }*/
    }
}
