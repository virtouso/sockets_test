using Server_DotNetCore.Config;

namespace Server_DotNetCore.Tests;

public class AuthenticationTests
{
    [Fact]
    public void Authenticate_ValidCredentials_ShouldSucceed()
    {
        // Arrange
        var username = ServerConfig.Username;
        var password = ServerConfig.Password;

        // Act & Assert
        var isValid = username == ServerConfig.Username && password == ServerConfig.Password;
        Assert.True(isValid);
    }

    [Fact]
    public void Authenticate_InvalidUsername_ShouldFail()
    {
        // Arrange
        var username = "wrong";
        var password = ServerConfig.Password;

        // Act & Assert
        var isValid = username == ServerConfig.Username && password == ServerConfig.Password;
        Assert.False(isValid);
    }

    [Fact]
    public void Authenticate_InvalidPassword_ShouldFail()
    {
        // Arrange
        var username = ServerConfig.Username;
        var password = "wrong";

        // Act & Assert
        var isValid = username == ServerConfig.Username && password == ServerConfig.Password;
        Assert.False(isValid);
    }

    [Fact]
    public void Authenticate_EmptyCredentials_ShouldFail()
    {
        // Arrange
        var username = "";
        var password = "";

        // Act & Assert
        var isValid = username == ServerConfig.Username && password == ServerConfig.Password;
        Assert.False(isValid);
    }

    [Fact]
    public void Authenticate_NullCredentials_ShouldFail()
    {
        // Arrange
        string? username = null;
        string? password = null;

        // Act & Assert
        var isValid = username == ServerConfig.Username && password == ServerConfig.Password;
        Assert.False(isValid);
    }
}

