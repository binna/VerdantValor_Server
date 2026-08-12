using System.Net;
using ChatServer.Client;

internal class Server
{
    public enum EClientMenu
    {
        Logout = 0,
        Login = 1,
        EnterWorld = 2,
        CreateParty = 3,
        DeleteParty = 4,
        EnterParty = 5,
        ExitParty = 6,
        WorldSendMessage = 7,
        PartySendMessage = 8,
        DirectSendMessage = 9,
    }
    
    static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();
        ChatSocketClient? client = null;
        Task? startTask = null;
        ulong userId = 0;
        string nickname = "Test";

        while (!cts.Token.IsCancellationRequested)
        {
            PrintMenu();

            if (Enum.TryParse<EClientMenu>(Console.ReadLine(), out var menu))
            {
                Console.WriteLine("[Notice] 잘못된 입력입니다.");
                continue;
            }

            switch (menu)
            {
                case EClientMenu.Login:
                    if (ulong.TryParse(Console.ReadLine(), out userId)) 
                    {
                        client = new ChatSocketClient(cts.Token);
                        startTask = client.StartAsync();
                        
                        var sessionId = Console.ReadLine();
                        if (sessionId is not null)
                        {
                            Console.WriteLine($"{sessionId}");
                            await client.LoginAsync(userId, sessionId);
                            break;
                        }
                    }
                    Console.WriteLine("Invalid input");
                    break;
                case EClientMenu.EnterWorld:
                    if (client is not null)
                    {
                        var worldName = Console.ReadLine();
                        if (worldName is not null)
                        {
                            Console.WriteLine($"{worldName}");
                            await client.EnterWorldAsync(worldName);
                        }
                    }
                    Console.WriteLine("Invalid input and no login");
                    break;
                case EClientMenu.CreateParty:
                    if (client is not null)
                    {
                        var partyId = Console.ReadLine();
                        if (partyId is not null)
                        {
                            Console.WriteLine($"{partyId}");
                            await client.CreatePartyAsync(partyId);
                        }
                    }
                    Console.WriteLine("Invalid input and no login");
                    break;
                case EClientMenu.DeleteParty:
                    if (client is not null)
                    {
                        var partyId = Console.ReadLine();
                        if (partyId is not null)
                        {
                            Console.WriteLine($"{partyId}");
                            await client.DeletePartyAsync(partyId);
                        }
                    }
                    Console.WriteLine("Invalid input and no login");
                    break;
                case EClientMenu.EnterParty:
                    if (client is not null)
                        await client.EnterPartyAsync();
                    Console.WriteLine("Invalid input and no login");
                    break;
                case EClientMenu.ExitParty:
                    if (client is not null)
                        await client.ExitPartyAsync();
                    Console.WriteLine("Invalid input and no login");
                    break;
                case EClientMenu.WorldSendMessage:
                    if (client is not null)
                    {
                        var message = Console.ReadLine();
                        if (message is not null)
                            await client.WorldSendMessage(message);
                    }
                    Console.WriteLine("Invalid input and no login");
                    break;
                case EClientMenu.PartySendMessage:
                    if (client is not null)
                    {
                        var message = Console.ReadLine();
                        if (message is not null)
                            await client.PartySendMessage(message);
                    }
                    Console.WriteLine("Invalid input and no login");
                    break;
                case EClientMenu.DirectSendMessage:
                    if (client is not null)
                    {
                        var message = Console.ReadLine();
                        if (message is not null)
                        {
                            if (userId != 0)
                                await client.DirectSendMessage(userId, nickname, 5, message);
                        }
                    }
                    Console.WriteLine("Invalid input and no login");
                    break;
                default:
                    Console.WriteLine("[Notice] 잘못된 입력입니다.");
                    break;
            }
        }
    }

    private static void PrintMenu()
    {
        Console.WriteLine("Menu=================");
        Console.WriteLine("1. Login");
        Console.WriteLine("2. Enter World");
        Console.WriteLine("3. Create Party");
        Console.WriteLine("4. Delete Party");
        Console.WriteLine("5. Enter Party");
        Console.WriteLine("6. Exit Party");
        Console.WriteLine("7. Send Message");
        Console.WriteLine("8. Disconnect");
        Console.WriteLine("0. Logout");
        Console.WriteLine("=====================");
        Console.Write("Menu Num: ");
    }
}