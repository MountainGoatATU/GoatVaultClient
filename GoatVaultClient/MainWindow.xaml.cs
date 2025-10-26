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
        public MainWindow()
        {
            InitializeComponent();
        }

        public async Task<string> HttpPost(string url, string jsonPayload)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                    "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("X-API-KEY", "PB7KTN_edJEz5oUdhTRpaz2T_-SpZj_C5ZvD2AWPcPc");

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);
                string responseData = await response.Content.ReadAsStringAsync();
                return responseData;
            }
        }

        public async Task<string> HttpGet(string url)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                    "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("X-API-KEY", "PB7KTN_edJEz5oUdhTRpaz2T_-SpZj_C5ZvD2AWPcPc");

                var response = await client.GetAsync(url);
                string responseData = await response.Content.ReadAsStringAsync();
                return responseData;
            }
        }

       
        // Create a single, static, RandomNumberGenerator instance to be used throughout the application.
        private static readonly RandomNumberGenerator Rng = System.Security.Cryptography.RandomNumberGenerator.Create();

        private static string CreateVault(string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] salt = new byte[16];
            Rng.GetBytes(salt);

            // Derive 32-byte key using Argon2id
            var config = new Argon2Config
            {
                Type = Argon2Type.DataIndependentAddressing,
                Version = Argon2Version.Nineteen,
                TimeCost = 10,
                MemoryCost = 32768,
                Lanes = 5,
                Threads = Environment.ProcessorCount,
                Password = passwordBytes,
                Salt = salt,
                HashLength = 32
            };
            var argon2 = new Argon2(config);
            byte[] derivedKey;

            using (SecureArray<byte> hash = argon2.Hash())
            {
                derivedKey = (byte[])hash.Buffer.Clone();
            }

            // Example vault content (JSON)
            var vaultData = new
            {
                entries = new[] {
                    new { site = "github.com", username = "alice", password = "ghp_secret" },
                    new { site = "gmail.com", username = "bob", password = "gmail_secret" }
                }
            };

            string vaultJson = JsonSerializer.Serialize(vaultData, new JsonSerializerOptions { WriteIndented = true });
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(vaultJson);

            // Encrypt using AES-256-GCM
            byte[] nonce = new byte[12]; 
            Rng.GetBytes(nonce);

            byte[] ciphertext = new byte[plaintextBytes.Length];
            byte[] authTag = new byte[16];

            using (var aesGcm = new AesGcm(derivedKey, 16))
            {
                // Encrypt the plaintext
                aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, authTag);
            }

            // Prepare payload for server
            var payload = new
            {
                name = "Testing Vault 2",
                salt = Convert.ToBase64String(salt),
                nonce = Convert.ToBase64String(nonce),
                encrypted_blob = Convert.ToBase64String(ciphertext),
                auth_tag = Convert.ToBase64String(authTag)
            };

            return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        }

        private static void DecryptVaultFromServer(string payloadJson, string password)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<ServerPayload>(payloadJson);

                // Decode base64 fields
                byte[] salt = Convert.FromBase64String(payload.salt);
                byte[] nonce = Convert.FromBase64String(payload.nonce);
                byte[] ciphertext = Convert.FromBase64String(payload.encrypted_blob);
                byte[] authTag = Convert.FromBase64String(payload.auth_tag); 

                // Derive the same key from password and salt
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

                var config = new Argon2Config
                {
                    Type = Argon2Type.DataIndependentAddressing,
                    Version = Argon2Version.Nineteen,
                    TimeCost = 10,
                    MemoryCost = 32768,
                    Lanes = 5,
                    Threads = Environment.ProcessorCount,
                    Password = passwordBytes,
                    Salt = salt,
                    HashLength = 32
                };
                var argon2 = new Argon2(config);
                byte[] derivedKey;
                using (SecureArray<byte> hash = argon2.Hash())
                {
                    derivedKey = (byte[])hash.Buffer.Clone();
                }

                // Attempt to decrypt
                byte[] decryptedBytes = new byte[ciphertext.Length];
                using (var aesGcm = new AesGcm(derivedKey, 16))
                {
                    aesGcm.Decrypt(nonce, ciphertext, authTag, decryptedBytes);
                }
                string decryptedJson = Encoding.UTF8.GetString(decryptedBytes);

                Console.WriteLine("Decryption successful!");
                Console.WriteLine(decryptedJson);
            }
            catch (CryptographicException)
            {
                Console.WriteLine("Decryption failed — incorrect password or data tampered.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }

        private static string RegisterUser(string email, string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] salt = new byte[16];
            Rng.GetBytes(salt);

            var config = new Argon2Config
            {
                Type = Argon2Type.DataIndependentAddressing,
                Version = Argon2Version.Nineteen,
                TimeCost = 10,
                MemoryCost = 32768,
                Lanes = 5,
                Threads = Environment.ProcessorCount,
                Password = passwordBytes,
                Salt = salt,
                HashLength = 32
            };

            var argon2 = new Argon2(config);
            string passwordHash;
            using (SecureArray<byte> hash = argon2.Hash())
            {
                passwordHash = Convert.ToBase64String(hash.Buffer);
            }

            var userPayload = new UserPayload
            {
                email = email,
                salt = Convert.ToBase64String(salt),
                password_hash = passwordHash,
                mfa_enabled = false,
                mfa_secret = null
            };


            return JsonSerializer.Serialize(userPayload, new JsonSerializerOptions { WriteIndented = true }); // return json
        }


        public class ServerPayload
        {
            public string user_id { get; set; }
            public string salt { get; set; }
            public string nonce { get; set; }
            public string encrypted_blob { get; set; }
            public string auth_tag { get; set; }
        }

        public class UserPayload
        {
            public string email { get; set; }
            public string salt { get; set; }
            public string password_hash { get; set; }
            public bool mfa_enabled { get; set; }
            public string? mfa_secret { get; set; }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            // Simulate user vault creation
            /*string password = "password1";
            string payloadJson = CreateVault(password);

            Console.WriteLine(payloadJson + "\n");*/

            // Send payload to server
            //string user_id = "b1c1f27a-cc59-4d2b-ae74-7b3b0e33a61a";
            //string serverUrl = "https://dev-api.mountaingoat.dev/v1/users/b1c1f27a-cc59-4d2b-ae74-7b3b0e33a61a/vaults/"; // Replace with actual server URL
            //string serverUrl = "http://127.0.0.1:8000/v1/users/b1c1f27a-cc59-4d2b-ae74-7b3b0e33a61a/vaults/"; // Local server for testing
            //string response = await HttpPost(serverUrl, payloadJson);

            // Simulate user decrypting vault after retrieval
            string password = "password1";
            string serverUrl = "http://127.0.0.1:8000/v1/users/b1c1f27a-cc59-4d2b-ae74-7b3b0e33a61a/vaults/64287312-d8e3-4617-b7ea-4b9cda640438";
            string payloadJson = await HttpGet(serverUrl);

            DecryptVaultFromServer(payloadJson, password);

            //DecryptVaultFromServer(payloadJson, "password1");
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
