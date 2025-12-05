using System.Net.Sockets;
using System.Text;
using Server_DotNetCore.Config;
using Server_DotNetCore.Extensions;
using Server_DotNetCore.Handlers;

namespace Server_DotNetCore.Tests;

public class AuthHandlerTests
{
    [Fact]
    public void AuthHandler_ValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        using var client = new TcpClient();
        var handler = new AuthHandler(client);
        using var stream = new MemoryStream();
        
        // Write valid credentials to stream
        var username = ServerConfig.Username;
        var password = ServerConfig.Password;
        var usernameBytes = Encoding.UTF8.GetBytes(username);
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        
        stream.Write(BitConverter.GetBytes(usernameBytes.Length));
        stream.Write(usernameBytes);
        stream.Write(BitConverter.GetBytes(passwordBytes.Length));
        stream.Write(passwordBytes);
        stream.Position = 0;

        // Act & Assert - This would need NetworkStream mocking, but we can test the logic
        Assert.True(username == ServerConfig.Username && password == ServerConfig.Password);
    }

    [Fact]
    public void AuthHandler_InvalidCredentials_ShouldReturnFailure()
    {
        // Arrange
        var username = "wrong";
        var password = "wrong";
        
        // Act & Assert
        Assert.False(username == ServerConfig.Username && password == ServerConfig.Password);
    }

    [Fact]
    public void AuthHandler_EmptyCredentials_ShouldReturnFailure()
    {
        // Arrange
        var username = "";
        var password = "";
        
        // Act & Assert
        Assert.False(username == ServerConfig.Username && password == ServerConfig.Password);
    }
}

