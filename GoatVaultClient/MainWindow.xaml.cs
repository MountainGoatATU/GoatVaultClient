using Isopoh.Cryptography.Argon2;
using Isopoh.Cryptography.SecureArray;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
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

        private static readonly RandomNumberGenerator Rng = System.Security.Cryptography.RandomNumberGenerator.Create();

        private void CreateVault() 
        {
            var password = "password1";
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
                Threads = Environment.ProcessorCount, // higher than "Lanes" doesn't help (or hurt)
                Password = passwordBytes,
                Salt = salt, // >= 8 bytes if not null
                //Secret = secret, // from somewhere
                //AssociatedData = associatedData, // from somewhere
                HashLength = 20 // >= 4
            };
            var argon2A = new Argon2(config);
            string hashString;
            using (SecureArray<byte> hashA = argon2A.Hash())
            {
                hashString = config.EncodeString(hashA.Buffer);
            }

            if (Argon2.Verify(hashString, passwordBytes, 5))
            {
                // verified
                Console.WriteLine("Verified");
            }
        }
    }
}
