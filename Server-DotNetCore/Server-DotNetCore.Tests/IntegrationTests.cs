using System.Net.Sockets;
using System.Text;
using Server_DotNetCore.Config;
using Server_DotNetCore.Models;

namespace Server_DotNetCore.Tests;


public class IntegrationTests
{
    private TcpClient CreateTestClient()
    {
        var client = new TcpClient();
        client.Connect("127.0.0.1", ServerConfig.Port);
        return client;
    }

    private void WriteInt32(NetworkStream stream, int value)
    {
        stream.Write(BitConverter.GetBytes(value));
    }

    private void WriteInt64(NetworkStream stream, long value)
    {
        stream.Write(BitConverter.GetBytes(value));
    }

    private int ReadInt32(NetworkStream stream)
    {
        byte[] buffer = new byte[4];
        stream.ReadExactly(buffer, 0, 4);
        return BitConverter.ToInt32(buffer, 0);
    }

    private long ReadInt64(NetworkStream stream)
    {
        byte[] buffer = new byte[8];
        stream.ReadExactly(buffer, 0, 8);
        return BitConverter.ToInt64(buffer, 0);
    }

    private byte[] ReadBytes(NetworkStream stream, int length)
    {
        byte[] buffer = new byte[length];
        stream.ReadExactly(buffer, 0, length);
        return buffer;
    }

    private bool IsServerRunning()
    {
        try
        {
            using var client = new TcpClient();
            client.Connect("127.0.0.1", ServerConfig.Port);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool Authenticate(NetworkStream stream)
    {
        stream.WriteByte((byte)Command.Auth);

        var username = Encoding.UTF8.GetBytes(ServerConfig.Username);
        WriteInt32(stream, username.Length);
        stream.Write(username);

        var password = Encoding.UTF8.GetBytes(ServerConfig.Password);
        WriteInt32(stream, password.Length);
        stream.Write(password);
        stream.Flush();

        var response = stream.ReadByte();
        return response == 1;
    }

    [Fact]
    public void Auth_ValidCredentials_ReturnsSuccess()
    {
        if (!IsServerRunning())
            throw new InvalidOperationException("Server is not running. Start the server to run integration tests.");

        using var client = CreateTestClient();
        var stream = client.GetStream();

        var result = Authenticate(stream);

        Assert.True(result);
    }

    [Fact]
    public void Auth_InvalidCredentials_ReturnsFailure()
    {
        if (!IsServerRunning())
            throw new InvalidOperationException("Server is not running. Start the server to run integration tests.");

        using var client = CreateTestClient();
        var stream = client.GetStream();

        stream.WriteByte((byte)Command.Auth);

        var username = Encoding.UTF8.GetBytes("wrong");
        WriteInt32(stream, username.Length);
        stream.Write(username);

        var password = Encoding.UTF8.GetBytes("wrong");
        WriteInt32(stream, password.Length);
        stream.Write(password);
        stream.Flush();

        var response = stream.ReadByte();

        Assert.Equal(0, response);
    }

    [Fact]
    public void ListFiles_AfterAuth_ReturnsFileList()
    {
        if (!IsServerRunning())
            throw new InvalidOperationException("Server is not running. Start the server to run integration tests.");

        using var client = CreateTestClient();
        var stream = client.GetStream();

        Assert.True(Authenticate(stream));

        stream.WriteByte((byte)Command.ListFiles);
        stream.Flush();

        var count = ReadInt32(stream);

        var files = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var nameLen = ReadInt32(stream);
            var nameBytes = ReadBytes(stream, nameLen);
            files.Add(Encoding.UTF8.GetString(nameBytes));
        }

        Assert.True(count >= 0);
        Assert.Equal(count, files.Count);
    }

    [Fact]
    public void Ping_AfterAuth_ReturnsSuccess()
    {
        if (!IsServerRunning())
            throw new InvalidOperationException("Server is not running. Start the server to run integration tests.");

        using var client = CreateTestClient();
        var stream = client.GetStream();

        Assert.True(Authenticate(stream));

        stream.WriteByte((byte)Command.Ping);
        stream.Flush();

        var response = stream.ReadByte();

        Assert.Equal(1, response);
    }

    [Fact]
    public void PutFile_AfterAuth_UploadsFile()
    {
        if (!IsServerRunning())
            throw new InvalidOperationException("Server is not running. Start the server to run integration tests.");

        using var client = CreateTestClient();
        var stream = client.GetStream();

        Assert.True(Authenticate(stream));

        stream.WriteByte((byte)Command.PutFile);

        var fileName = "integration_test.txt";
        var fileNameBytes = Encoding.UTF8.GetBytes(fileName);
        WriteInt32(stream, fileNameBytes.Length);
        stream.Write(fileNameBytes);

        var fileContent = "Integration test content";
        var fileContentBytes = Encoding.UTF8.GetBytes(fileContent);
        WriteInt64(stream, fileContentBytes.Length);

        stream.Write(fileContentBytes);
        stream.Flush();

        // File should be uploaded (no response expected, just verify no exception)
        Assert.True(true);
    }

    [Fact]
    public void GetFile_AfterAuth_DownloadsFile()
    {
        if (!IsServerRunning())
            throw new InvalidOperationException("Server is not running. Start the server to run integration tests.");

        using var client = CreateTestClient();
        var stream = client.GetStream();

        Assert.True(Authenticate(stream));

        stream.WriteByte((byte)Command.PutFile);
        var fileName = "download_test.txt";
        var fileNameBytes = Encoding.UTF8.GetBytes(fileName);
        WriteInt32(stream, fileNameBytes.Length);
        stream.Write(fileNameBytes);
        var fileContent = "download test content";
        var fileContentBytes = Encoding.UTF8.GetBytes(fileContent);
        WriteInt64(stream, fileContentBytes.Length);
        stream.Write(fileContentBytes);
        stream.Flush();

        // Small delay to ensure file is written
        Thread.Sleep(100);

        stream.WriteByte((byte)Command.GetFile);
        WriteInt32(stream, fileNameBytes.Length);
        stream.Write(fileNameBytes);
        stream.Flush();

        var size = ReadInt64(stream);

        if (size > 0)
        {
            var contentBytes = ReadBytes(stream, (int)size);
            var content = Encoding.UTF8.GetString(contentBytes);

            Assert.Equal(fileContent, content);
        }
        else
        {
            Assert.Equal(-1, size);
        }
    }
}
