using System.Net;

namespace ChatServer;

internal class Server
{
    static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();
        var server = new ChatSocketServer(cts);
        await server.StartAsync(IPAddress.Any, 20000);
    }
}
