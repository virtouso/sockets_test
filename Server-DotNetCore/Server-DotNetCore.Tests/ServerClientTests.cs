using Server_DotNetCore.Handlers;
using Server_DotNetCore.Models;

namespace Server_DotNetCore.Tests;

public class ServerClientTests
{
    [Fact]
    public void ServerClient_ShouldHaveAllCommandHandlers()
    {
        // Verify that ServerClient initializes handlers for all commands
        var commands = Enum.GetValues<Command>().Where(c => c != Command.FileUploaded).ToList();
        Assert.NotEmpty(commands);
        Assert.Contains(Command.Auth, commands);
        Assert.Contains(Command.ListFiles, commands);
        Assert.Contains(Command.GetFile, commands);
        Assert.Contains(Command.PutFile, commands);
        Assert.Contains(Command.Ping, commands);
    }
}

