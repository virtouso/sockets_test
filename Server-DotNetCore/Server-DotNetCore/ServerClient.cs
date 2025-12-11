using System.Net.Sockets;
using Server_DotNetCore.Extensions;
using Server_DotNetCore.Handlers;
using Server_DotNetCore.Models;

namespace Server_DotNetCore;

public class ServerClient
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly Dictionary<Command, ICommandHandler> _handlers;

    public ServerClient(TcpClient client)
    {
		_client = client;
		_stream = client.GetStream();
		
		_handlers = new Dictionary<Command, ICommandHandler>
        {
            { Command.Auth, new AuthHandler(_client) },
            { Command.ListFiles, new ListFilesHandler() },
            { Command.GetFile, new GetFileHandler() },
            { Command.PutFile, new PutFileHandler(this) },
            { Command.Ping, new PingHandler() }
        };
    } 

    public NetworkStream Stream => _stream;

    public async Task StartAsync()
    {
        while (true)
        {
            byte cmdByte = await SocketExtensions.ReadByteAsync(_stream);
            var cmd = (Command)cmdByte;

            if (_handlers.TryGetValue(cmd, out var handler))
            {
                Console.WriteLine("Message Received");
                await handler.HandleAsync(_stream);
            }
            else
            {
                throw new Exception("Unknown command: " + cmdByte);
            }
        }
    }
}