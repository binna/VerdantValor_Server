using System.Net;
using ChatServer.Client;

internal class Server
{
    static async Task Main(string[] args)
    {
        var server = new SocketClient(IPAddress.Loopback, 20000);

        using var cts = new CancellationTokenSource();
        
        await server.StartAsync(cts.Token);
    }
}