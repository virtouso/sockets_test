using System.Net;
using System.Net.Sockets;
using Server_DotNetCore.Config;
using Server_DotNetCore.Services;
using System.Linq;

namespace Server_DotNetCore;

class Program
{
    private const int MaxClients = 32;
    private static readonly SemaphoreSlim ClientLimit = new SemaphoreSlim(MaxClients);
    private static readonly HashSet<ServerClient> ConnectedClients = new HashSet<ServerClient>();
    private static readonly object ClientsLock = new object();
    private static RedisService? _redisService;

    static async Task Main(string[] args)
    {
        Directory.CreateDirectory("server_files");

        // Initialize Redis connection
        var redisConnectionString = Environment.GetEnvironmentVariable("Redis__ConnectionString") ?? "localhost:6379";
        _redisService = new RedisService(redisConnectionString);

        // Initialize file count in Redis
        await UpdateFileCountInRedisAsync();

        TcpListener listener = new TcpListener(IPAddress.Any, ServerConfig.Port);
        listener.Start();

        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Shutdown requested...");
            e.Cancel = true;
            cts.Cancel();
            _redisService?.Dispose();
        };

        Console.WriteLine($"TCP server listening on port {ServerConfig.Port} (max {MaxClients} clients)");

        while (!cts.Token.IsCancellationRequested)
        {
            var acceptTask = listener.AcceptTcpClientAsync();
            var completed = await Task.WhenAny(acceptTask, Task.Delay(200, cts.Token));

            if (completed != acceptTask)
                continue;

            TcpClient client = acceptTask.Result;

            if (!await ClientLimit.WaitAsync(0))
            {
                Console.WriteLine("Too many clients — refusing connection.");
                using (var ns = client.GetStream())
                    await ns.WriteAsync(System.Text.Encoding.UTF8.GetBytes("BUSY"));
                client.Close();
                continue;
            }

            _ = Task.Run(async () =>
            {
                ServerClient? sc = null;
                try
                {
                    Console.WriteLine("Client connected: " + client.Client.RemoteEndPoint);
                    sc = new ServerClient(client);
                    lock (ClientsLock)
                    {
                        ConnectedClients.Add(sc);
                    }
                    await UpdateActiveUsersInRedisAsync();
                    await sc.StartAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Client error: " + ex.Message);
                }
                finally
                {
                    if (sc != null)
                    {
                        lock (ClientsLock)
                        {
                            ConnectedClients.Remove(sc);
                        }
                        await UpdateActiveUsersInRedisAsync();
                    }
                    client.Close();
                    ClientLimit.Release();
                    Console.WriteLine("Client disconnected.");
                }
            }, cts.Token);
        }

        Console.WriteLine("TCP Server shutting down...");
        listener.Stop();
    }

    public static List<ServerClient> GetConnectedClients()
    {
        lock (ClientsLock)
        {
            return ConnectedClients.ToList();
        }
    }

    public static async Task UpdateFileCountInRedisAsync()
    {
        if (_redisService == null) return;
        
        try
        {
            string[] files = Directory.GetFiles("server_files");
            await _redisService.UpdateFileCountAsync(files.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating file count in Redis: {ex.Message}");
        }
    }

    public static async Task UpdateActiveUsersInRedisAsync()
    {
        if (_redisService == null) return;
        
        try
        {
            int count = GetConnectedClients().Count;
            await _redisService.UpdateActiveUsersAsync(count);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating active users in Redis: {ex.Message}");
        }
    }
}
