using System.Net.Sockets;
using System.Text;
using Server_DotNetCore.Config;
using Server_DotNetCore.Extensions;

namespace Server_DotNetCore.Handlers;

public class AuthHandler : ICommandHandler
{
    private readonly TcpClient _client;

    public AuthHandler(TcpClient client)
    {
        _client = client;
    }

    public async Task HandleAsync(NetworkStream stream)
    {
        Console.WriteLine("Received Auth Request");
        int usernameLen = await SocketExtensions.ReadInt32Async(stream);
        var username = Encoding.UTF8.GetString(await SocketExtensions.ReadBytesAsync(stream, usernameLen));

        int passwordLen = await SocketExtensions.ReadInt32Async(stream);
        var password = Encoding.UTF8.GetString(await SocketExtensions.ReadBytesAsync(stream, passwordLen));

        if (username == ServerConfig.Username && password == ServerConfig.Password)
        {
            await stream.WriteAsync(new byte[] { 1 });
        }
        else
        {
            await stream.WriteAsync(new byte[] { 0 });
            _client.Close();
        }
    }
}

