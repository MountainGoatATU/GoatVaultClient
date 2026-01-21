using GoatVaultClient_v3.Models;
using GoatVaultClient_v3.Services;
using PasswordGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClientTests
{
    [TestFixture]
    public class UserServiceTests
    {
        private UserService _userService;
        private VaultService _vaultService;
        [SetUp]
        public void Setup()
        {
            _userService = new UserService();
            _vaultService = new VaultService(
                goatVaultDB: null,
                httpService: null,
                vaultSessionService: null
            );
        }

        [Test]
        public void RegisterUser_ValidInput_ReturnsAuthRegisterRequest()
        {
            // Arrange
            var email = "test@gmail.com";
            var password = "StrongPassword123!";

            // Act
            var registerRequest = _userService.RegisterUser(email, password, null);
            var vaultPayload = _vaultService.EncryptVault(password, null);
            registerRequest.Vault = vaultPayload;

            // Assert
            Assert.That(registerRequest, Is.Not.Null);
            Assert.That(registerRequest.Email, Is.EqualTo(email));
            Assert.That(registerRequest.AuthSalt, Is.Not.Null.And.Not.Empty);
            Assert.That(registerRequest.AuthVerifier, Is.Not.Null.And.Not.Empty);
            Assert.That(registerRequest.Vault, Is.Not.Null);
        }

        [Test]
        public void GenerateAuthVerifier_CorrectPassword_ReturnsMatchingVerifier()
        {
            // Arrange
            var email = "test@gmail.com";
            var password = "StrongPassword123!";

            // Act
            var registerRequest = _userService.RegisterUser(email, password, null);

            var generatedVerifier = _userService.GenerateAuthVerifier(password, registerRequest.AuthSalt);

            // Assert
            Assert.That(generatedVerifier, Is.EqualTo(registerRequest.AuthVerifier));
        }

        [Test]
        public void GenerateAuthVerifier_WrongPassword_ReturnsNonMatchingVerifier()
        {
            // Arrange
            var email = "test@gmail.com";
            var password = "StrongPassword123!";

            // Act
            var registerRequest = _userService.RegisterUser(email, password, null);

            var generatedVerifier = _userService.GenerateAuthVerifier("WrongPassword!", registerRequest.AuthSalt);

            // Assert
            Assert.That(generatedVerifier, Is.Not.EqualTo(registerRequest.AuthVerifier));
        }
    }
}
