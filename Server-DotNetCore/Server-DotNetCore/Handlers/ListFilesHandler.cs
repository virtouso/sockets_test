using System.Net.Sockets;
using System.Text;
using Server_DotNetCore.Extensions;

namespace Server_DotNetCore.Handlers;

public class ListFilesHandler : ICommandHandler
{
    public async Task HandleAsync(NetworkStream stream)
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
}

