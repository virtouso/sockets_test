using Server_DotNetCore.Handlers;
using Server_DotNetCore.Models;

namespace Server_DotNetCore.Tests;

public class CommandHandlerTests
{
    [Fact]
    public void AuthHandler_ImplementsICommandHandler()
    {
        var handler = new AuthHandler(new System.Net.Sockets.TcpClient());
        Assert.IsAssignableFrom<ICommandHandler>(handler);
    }

    [Fact]
    public void ListFilesHandler_ImplementsICommandHandler()
    {
        var handler = new ListFilesHandler();
        Assert.IsAssignableFrom<ICommandHandler>(handler);
    }

    [Fact]
    public void GetFileHandler_ImplementsICommandHandler()
    {
        var handler = new GetFileHandler();
        Assert.IsAssignableFrom<ICommandHandler>(handler);
    }

    [Fact]
    public void PutFileHandler_ImplementsICommandHandler()
    {
        var handler = new PutFileHandler();
        Assert.IsAssignableFrom<ICommandHandler>(handler);
    }

    [Fact]
    public void PingHandler_ImplementsICommandHandler()
    {
        var handler = new PingHandler();
        Assert.IsAssignableFrom<ICommandHandler>(handler);
    }
}

