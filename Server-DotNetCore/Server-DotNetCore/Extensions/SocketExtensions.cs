using System.Net.Sockets;

namespace Server_DotNetCore.Extensions;

public static class SocketExtensions
{
    public static Task<byte> ReadByteAsync(NetworkStream stream)
    {
        int b = stream.ReadByte();
        if (b == -1) throw new IOException("Disconnected");
        return Task.FromResult((byte)b);
    }

    public static async Task<int> ReadInt32Async(NetworkStream stream)
    {
        byte[] buffer = new byte[4];
        await stream.ReadExactlyAsync(buffer, 0, 4);
        return BitConverter.ToInt32(buffer, 0);
    }

    public static async Task<long> ReadInt64Async(NetworkStream stream)
    {
        byte[] buffer = new byte[8];
        await stream.ReadExactlyAsync(buffer, 0, 8);
        return BitConverter.ToInt64(buffer, 0);
    }

    public static async Task<byte[]> ReadBytesAsync(NetworkStream stream, int length)
    {
        byte[] buffer = new byte[length];
        await stream.ReadExactlyAsync(buffer, 0, length);
        return buffer;
    }
}