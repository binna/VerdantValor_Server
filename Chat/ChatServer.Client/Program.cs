using System.Net;
using ChatServer.Client;

internal class Server
{
    static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();
        var server = new ChatSocketClient(IPAddress.Loopback, 20000, cts.Token);

        var startTask = server.StartAsync();
        
        Console.WriteLine("Menu=================");
        Console.WriteLine("1. Login");
        Console.WriteLine("2. Show RoomList");
        Console.WriteLine("3. Create Room");
        Console.WriteLine("4. Enter Room");
        Console.WriteLine("5. Terminate a Program");
        Console.WriteLine("=====================");
        
        while (!cts.Token.IsCancellationRequested) 
        {
            Console.WriteLine("Menu Num: ");
            var selectedOption = Console.ReadLine();

            switch (selectedOption)
            {
                case "1":
                {
                    Console.WriteLine("Enter UserId: ");
                    var userIdInput = Console.ReadLine();
                    if (!ulong.TryParse(userIdInput, out var userId))
                    {
                        Console.WriteLine("Invalid user ID.");
                        break;
                    }

                    await server.SendLoginReqAsync(userId);
                    break;
                }
                case "2":
                {
                    await server.SendRoomListReqAsync();
                    break;
                }
                case "3":
                {
                    await server.SendCreateRoomAsync();
                    break;
                }
                case "4":
                {
                    Console.WriteLine("Enter RoomId : ");
                    var roomIdInput = Console.ReadLine();
                    if (!int.TryParse(roomIdInput, out var roomId))
                    {
                        Console.WriteLine("Invalid room ID.");
                        break;
                    }

                    await server.SendEnterRoomAsync(roomId);
                    break;
                }
                // TODO 5
            }
        }
    }
}