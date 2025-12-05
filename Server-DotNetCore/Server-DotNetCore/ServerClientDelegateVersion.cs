using System.Net.Sockets;
using System.Text;
using Server_DotNetCore.Extensions;
using Server_DotNetCore.Models;

namespace Server_DotNetCore;

public class ServerClientDelegateVersion
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly Dictionary<Command, Func<NetworkStream, Task>> _handlers;

    public ServerClientDelegateVersion(TcpClient client)
    {
        _client = client;
		_stream = client.GetStream();
		
		_handlers = new Dictionary<Command, Func<NetworkStream, Task>>
        {
            { Command.Auth, HandleAuth },
            { Command.ListFiles, HandleListFiles },
            { Command.GetFile, HandleGetFile },
            { Command.PutFile, HandlePutFile },
            { Command.Ping, HandlePing }
        };
    }

    public async Task StartAsync()
    {
        while (true)
        {
            byte cmdByte = await SocketExtensions.ReadByteAsync(_stream);
            var cmd = (Command)cmdByte;

            if (_handlers.TryGetValue(cmd, out var handler))
            {
                await handler(_stream);
            }
            else
            {
                throw new Exception("Unknown command: " + cmdByte);
            }
		}
	}

	private async Task HandleAuth(NetworkStream stream)
    {
        int usernameLen = await SocketExtensions.ReadInt32Async(stream);
        var username = Encoding.UTF8.GetString(await SocketExtensions.ReadBytesAsync(stream, usernameLen));

        int passwordLen = await SocketExtensions.ReadInt32Async(stream);
        var password = Encoding.UTF8.GetString(await SocketExtensions.ReadBytesAsync(stream, passwordLen));

        bool ok = username == "admin" && password == "123";

        if (ok)
        {
            await stream.WriteAsync(new byte[] { 1 });
        }
        else
        {
            await stream.WriteAsync(new byte[] { 0 });
            _client.Close();
        }
    }

    private async Task HandleListFiles(NetworkStream stream)
    {
        string[] files = Directory.GetFiles("server_files");

        byte[] countBytes = BitConverter.GetBytes(files.Length);
        await stream.WriteAsync(countBytes);

        foreach (var f in files)
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(f));
            await stream.WriteAsync(BitConverter.GetBytes(nameBytes.Length));
            await stream.WriteAsync(nameBytes);
        }
    }

    private async Task HandleGetFile(NetworkStream stream)
    {
        int nameLen = await SocketExtensions.ReadInt32Async(stream);
        string fileName = Encoding.UTF8.GetString(await SocketExtensions.ReadBytesAsync(stream, nameLen));

        string fullPath = Path.Combine("server_files", fileName);
        
        if (!File.Exists(fullPath))
        {
            await stream.WriteAsync(BitConverter.GetBytes((long)-1));
            return;
        }

        long size = new FileInfo(fullPath).Length;
        await stream.WriteAsync(BitConverter.GetBytes(size));

        using var fs = File.OpenRead(fullPath);
        byte[] buffer = new byte[8192];

        int read;
        while ((read = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
            await stream.WriteAsync(buffer.AsMemory(0, read));
    }

    private async Task HandlePutFile(NetworkStream stream)
    {
        int nameLen = await SocketExtensions.ReadInt32Async(stream);
        string fileName = Encoding.UTF8.GetString(await SocketExtensions.ReadBytesAsync(stream, nameLen));

        long fileSize = await SocketExtensions.ReadInt64Async(stream);

        string fullPath = Path.Combine("server_files", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using var fs = File.Create(fullPath);
        byte[] buffer = new byte[8192];
        long received = 0;

        while (received < fileSize)
        {
            int toRead = (int)Math.Min(buffer.Length, fileSize - received);
            int read = await stream.ReadAsync(buffer, 0, toRead);
            if (read == 0) throw new IOException("Connection closed during file upload");
            
            await fs.WriteAsync(buffer, 0, read);
            received += read;
        }

        // Note: Notification not supported in this alternative implementation
        // Use ServerClient for full notification support
    }

    private async Task HandlePing(NetworkStream stream)
    {
        await stream.WriteAsync(new byte[] { 1 });
    }
}

