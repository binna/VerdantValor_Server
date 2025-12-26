using System.Net;
using SharedLibrary.Tcp;

namespace ChatServer;

internal class Server
{
    static async Task Main(string[] args)
    {
        var server = new SocketServer(IPAddress.Any, 20000);

        using var cts = new CancellationTokenSource();
        
        await server.StartAsync(cts.Token);
    }
}