using Isopoh.Cryptography.Argon2;
using Isopoh.Cryptography.SecureArray;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
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
            CreateVault();
        }

        // Create a single, static, RandomNumberGenerator instance to be used throughout the application.
        private static readonly RandomNumberGenerator Rng = System.Security.Cryptography.RandomNumberGenerator.Create();


        private void CreateVault() 
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
            }*/
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

            // Encrypt the vault data using AES-GCM
            byte[] ciphertext = new byte[plaintextBytes.Length];
            // ciphertext will hold the encrypted data
            // thats why its the same length as plaintextBytes
            byte[] authTag = new byte[16];

            using (var aes = new AesGcm(derivedKey))
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

            string payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

        }
    }
}
