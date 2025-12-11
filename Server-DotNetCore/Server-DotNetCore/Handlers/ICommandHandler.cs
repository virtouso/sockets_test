using System.Net.Sockets;

namespace Server_DotNetCore.Handlers;

public interface ICommandHandler
{
    Task HandleAsync(NetworkStream stream);
}




