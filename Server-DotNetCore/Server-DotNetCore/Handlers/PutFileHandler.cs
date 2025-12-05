using System.Net.Sockets;
using System.Text;
using Server_DotNetCore.Extensions;
using Server_DotNetCore.Models;

namespace Server_DotNetCore.Handlers;

public class PutFileHandler : ICommandHandler
{
    private readonly ServerClient? _currentClient;

    public PutFileHandler(ServerClient? currentClient = null)
    {
        _currentClient = currentClient;
    }

    public async Task HandleAsync(NetworkStream stream)
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

        // Notify other clients about the upload
        await NotifyOtherClientsAsync(fileName);
    }

    private async Task NotifyOtherClientsAsync(string fileName)
    {
        // Only notify if we have a current client reference (main ServerClient implementation)
        if (_currentClient == null) return;
        
        var clients = Program.GetConnectedClients();
        byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
        var currentStream = _currentClient.Stream;
        
        foreach (var client in clients)
        {
            if (client.Stream == currentStream) continue; // Don't notify the uploader
            
            try
            {
                var stream = client.Stream;
                stream.WriteByte((byte)Command.FileUploaded);
                
                byte[] lengthBytes = BitConverter.GetBytes(fileNameBytes.Length);
                await stream.WriteAsync(lengthBytes, 0, 4);
                await stream.WriteAsync(fileNameBytes, 0, fileNameBytes.Length);
                await stream.FlushAsync();
            }
            catch (Exception ex)
            {
                // Client might have disconnected, ignore
                Console.WriteLine($"Failed to notify client: {ex.Message}");
            }
        }
    }
}

