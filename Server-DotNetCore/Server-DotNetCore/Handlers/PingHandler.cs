using System.Net.Sockets;

namespace Server_DotNetCore.Handlers;

public class PingHandler : ICommandHandler
{
    public async Task HandleAsync(NetworkStream stream)
    {
        await stream.WriteAsync(new byte[] { 1 });
    }
}

