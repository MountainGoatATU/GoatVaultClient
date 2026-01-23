using GoatVaultClient_v3.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClientTests
{
    [TestFixture]
    public class PasswordStrength
    {
        private PasswordStrengthService _passwordStrengthService;

        [SetUp]
        public void Setup()
        {
            _passwordStrengthService = new PasswordStrengthService();
        }

        [Test]
        public void Evaluate_StrongPassword_ReturnsHighScore()
        {
            // Arrange
            var password = "IatePizzaTodayAndItWasGood123!";

            // Act
            var result = _passwordStrengthService.Evaluate(password);

            // Assert
            Assert.That(result.Score, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void Evaluate_WeakPassword_ReturnsLowScore()
        {
            // Arrange
            var password = "12345";

            // Act
            var result = _passwordStrengthService.Evaluate(password);

            // Assert
            Assert.That(result.Score, Is.LessThanOrEqualTo(1));
        }

        [Test]
        public void Evaluate_EmptyPassword_ReturnsZeroScore()
        {
            // Arrange
            var password = "";

            // Act
            var result = _passwordStrengthService.Evaluate(password);

            // Assert
            Assert.That(result.Score, Is.EqualTo(0));
        }

        [Test]
        public void Evaluate_NullPassword_ReturnsZeroScore()
        {
            // Arrange
            string password = null;
            // Act
            var result = _passwordStrengthService.Evaluate(password);
            // Assert
            Assert.That(result.Score, Is.EqualTo(0));
        }
    }
}
