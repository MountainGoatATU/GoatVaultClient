namespace GoatVaultTests.Infrastructure;

public class UserRepositoryTests
{
    // TODO: Broken Tests
    /*
    private readonly UserService _userService = new();

    [Fact]
    public void RegisterUser_ValidInput_GeneratesAuthMaterial()
    {
        // Arrange
        const string email = "test@gmail.com";
        const string password = "StrongPassword123!";

        // Act
        var result = _userService.RegisterUser(email, password, vault: null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        Assert.False(string.IsNullOrWhiteSpace(result.AuthSalt));
        Assert.False(string.IsNullOrWhiteSpace(result.AuthVerifier));
    }

    [Fact]
    public void RegisterUser_SameCredentials_GeneratesDifferentSalt()
    {
        // Arrange
        const string email = "test@gmail.com";
        const string password = "StrongPassword123!";

        // Act
        var result1 = _userService.RegisterUser(email, password, null);
        var result2 = _userService.RegisterUser(email, password, null);

        // Assert
        Assert.NotEqual(result1.AuthSalt, result2.AuthSalt);
    }

    [Fact]
    public void RegisterUser_AuthVerifier_MatchesHashOfPasswordAndSalt()
    {
        // Arrange
        const string email = "test@gmail.com";
        const string password = "StrongPassword123!";

        // Act
        var result = _userService.RegisterUser(email, password, null);

        // Recreate expected verifier
        var saltBytes = Convert.FromBase64String(result.AuthSalt);
        var expectedVerifier = CryptoService.HashPassword(password, saltBytes);

        // Assert
        Assert.Equal(expectedVerifier, result.AuthVerifier);
    }

    [Fact]
    public void RegisterUser_AuthSalt_IsValidBase64()
    {
        // Arrange
        const string email = "test@gmail.com";
        const string password = "StrongPassword123!";

        // Act
        var result = _userService.RegisterUser(email, password, null);

        // Assert (will throw if invalid)
        var bytes = Convert.FromBase64String(result.AuthSalt);
        Assert.NotEmpty(bytes);
    }
    */
}