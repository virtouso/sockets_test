using System.Net;
using System.Net.Sockets;
using Server_DotNetCore.Config;
using System.Linq;

namespace Server_DotNetCore;

class Program
{
    private const int MaxClients = 32;
    private static readonly SemaphoreSlim ClientLimit = new SemaphoreSlim(MaxClients);
    private static readonly HashSet<ServerClient> ConnectedClients = new HashSet<ServerClient>();
    private static readonly object ClientsLock = new object();

    static async Task Main(string[] args)
    {
        Directory.CreateDirectory("server_files");

        TcpListener listener = new TcpListener(IPAddress.Any, ServerConfig.Port);
        listener.Start();

        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Shutdown requested...");
            e.Cancel = true;
            cts.Cancel();
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
}
