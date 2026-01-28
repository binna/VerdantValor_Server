using System.Net;

namespace ChatServer;

// TODO 여러개의 유저 테스트 필요

internal class Server
{
    static async Task Main(string[] args)
    {
        var server = new ChatSocketServer(IPAddress.Any, 20000);
        await server.StartAsync();
    }
}