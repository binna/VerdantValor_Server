using System.Net;
using ChatServer.Client;

internal class Server
{
    static async Task Main(string[] args)
    {
        var server = new SocketClient(IPAddress.Loopback, 20000);
        using var cts = new CancellationTokenSource();
        _ = server.StartAsync(cts.Token);
        
        Console.WriteLine("Menu=================");
        Console.WriteLine("1. Login");
        Console.WriteLine("2. Show RoomList");
        Console.WriteLine("3. Create Room");
        Console.WriteLine("4. Enter Room");
        Console.WriteLine("5. Terminate a Program");
        Console.WriteLine("=====================");
        
        while (!cts.Token.IsCancellationRequested) 
        {
            var selectedOption = Console.ReadLine();

            switch (selectedOption)
            {
                case "1":
                {
                    await server.SendLoginAsync();
                    break;
                }
                case "2":
                {
                    await server.SendRoomListAsync();
                    break;
                }
                case "3":
                {
                    await server.SendCreateRoomAsync();
                    break;
                }
                case "4":
                {
                    await server.SendEnterRoomAsync();
                    break;
                }
                default:
                    server.Stop();
                    return;
            }
        }
    }
}