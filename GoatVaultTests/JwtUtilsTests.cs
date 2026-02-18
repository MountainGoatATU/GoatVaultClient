using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Xunit;
using GoatVaultInfrastructure.Services.API;
using GoatVaultCore.Models.API;
using Microsoft.IdentityModel.Tokens;

namespace GoatVaultTests
{
    public class JwtUtilsTests
    {
        private readonly JwtUtils _jwtUtils = new JwtUtils();

        // Helper method to generate a test JWT
        private string GenerateTestJwt(string key = "SuperPizzaDelivery#147863331_0??Today")
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
                new Claim(JwtRegisteredClaimNames.Email, "test@example.com"),
                new Claim("customClaim", "customValue")
            };

            var token = new JwtSecurityToken(
                issuer: "TestIssuer",
                audience: "TestAudience",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Fact]
        public void ConvertJwtStringToJwtSecurityToken_ValidJwt_ReturnsJwtSecurityToken()
        {
            // Arrange
            var jwtString = GenerateTestJwt();

            // Act
            var token = _jwtUtils.ConvertJwtStringToJwtSecurityToken(jwtString);

            // Assert
            Assert.NotNull(token);
            Assert.Equal("TestIssuer", token.Issuer);
            Assert.Contains("TestAudience", token.Audiences);
            Assert.Equal("test-user", token.Subject);
        }

        [Fact]
        public void ConvertJwtStringToJwtSecurityToken_NullJwt_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _jwtUtils.ConvertJwtStringToJwtSecurityToken(null));
        }

        [Fact]
        public void ConvertJwtStringToJwtSecurityToken_InvalidJwt_ThrowsArgumentException()
        {
            // Arrange
            var invalidJwt = "invalid.token.value";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _jwtUtils.ConvertJwtStringToJwtSecurityToken(invalidJwt));
        }

        [Fact]
        public void DecodeToken_ValidJwtSecurityToken_ReturnsDecodedToken()
        {
            // Arrange
            var jwtString = GenerateTestJwt();
            var token = _jwtUtils.ConvertJwtStringToJwtSecurityToken(jwtString);

            // Act
            var decoded = _jwtUtils.DecodeToken(token);

            // Assert
            Assert.NotNull(decoded);
            Assert.Equal("TestIssuer", decoded.Issuer);
            Assert.Contains("TestAudience", decoded.Audience);
            Assert.Equal("test-user", decoded.Subject);

            var customClaim = decoded.Claims.FirstOrDefault(c => c.Item1 == "customClaim");
            Assert.NotNull(customClaim);
            Assert.Equal("customValue", customClaim.Item2);
        }

        [Fact]
        public void DecodeToken_NullToken_ThrowsNullReferenceException()
        {
            // Act & Assert
            Assert.Throws<NullReferenceException>(() => _jwtUtils.DecodeToken(null!));
        }
    }
}