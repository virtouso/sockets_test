using System.Net.Sockets;
using System.Text;
using Server_DotNetCore.Extensions;

namespace Server_DotNetCore.Handlers;

public class GetFileHandler : ICommandHandler
{
    public async Task HandleAsync(NetworkStream stream)
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
        
        // Ensure all data is flushed to the network
        await stream.FlushAsync();
    }
}

