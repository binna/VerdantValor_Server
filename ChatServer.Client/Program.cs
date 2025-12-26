using ChatServer.Client;

internal class Server
{
    static async Task Main(string[] args)
    {
        var server = new SocketClient(20000);

        using var cts = new CancellationTokenSource();
        
        await server.StartAsync(cts.Token);
    }
}