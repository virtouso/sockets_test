using System.Net.Sockets;
using Server_DotNetCore.Extensions;
using Server_DotNetCore.Handlers;
using Server_DotNetCore.Models;

namespace Server_DotNetCore;

public class ServerClientFactoryVersion
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly Dictionary<Command, Func<ICommandHandler>> _handlerFactories;
    private readonly ServerClient _serverClientWrapper;

    public ServerClientFactoryVersion(TcpClient client)
    {
        _client = client;
		_stream = client.GetStream();
		
		// Create a ServerClient instance for handlers that need it
		// Note: This creates handlers in ServerClient, but we use our own factory pattern
		_serverClientWrapper = new ServerClient(client);
		
		_handlerFactories = new Dictionary<Command, Func<ICommandHandler>>
        {
            { Command.Auth, () => new AuthHandler(_client) },
            { Command.ListFiles, () => new ListFilesHandler() },
            { Command.GetFile, () => new GetFileHandler() },
            { Command.PutFile, () => new PutFileHandler(_serverClientWrapper) },
            { Command.Ping, () => new PingHandler() }
        };
    }

    public async Task StartAsync()
    {
        while (true)
        {
            byte cmdByte = await SocketExtensions.ReadByteAsync(_stream);
            var cmd = (Command)cmdByte;

			if (_handlerFactories.TryGetValue(cmd, out var factory))
			{
				var handler = factory();
                await handler.HandleAsync(_stream);
            }
            else
            {
                throw new Exception("Unknown command: " + cmdByte);
            }
        }
    }
}

