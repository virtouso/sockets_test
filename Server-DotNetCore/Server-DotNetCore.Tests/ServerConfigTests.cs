using Server_DotNetCore.Config;

namespace Server_DotNetCore.Tests;

public class ServerConfigTests
{
    [Fact]
    public void Username_ShouldBeAdmin()
    {
        Assert.Equal("admin", ServerConfig.Username);
    }

    [Fact]
    public void Password_ShouldBe123()
    {
        Assert.Equal("123", ServerConfig.Password);
    }

    [Fact]
    public void Port_ShouldBe8080()
    {
        Assert.Equal(8080, ServerConfig.Port);
    }
}

