namespace GoatVaultTests;

public class UserServiceTests
{
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
}