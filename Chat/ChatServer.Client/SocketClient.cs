using System.Net;
using System.Net.Sockets;
using Tcp;
using VerdantValorShared.Common.ChatServer;
using VerdantValorShared.Packet;
using VerdantValorShared.Packet.ChatServer;

namespace ChatServer.Client;

public class SocketClient : SocketServer
{
    private readonly TcpClient mClient;
    private readonly SocketContext mSocketContext;
    
    public SocketClient(IPAddress ipAddress, int port, CancellationToken cancellationToken = default) 
        : base(
            new Dictionary<AppEnum.PacketType, Func<SocketContext, CancellationToken, Task>>
            {
                [AppEnum.PacketType.RoomList] = HandleRoomListAsync,
                [AppEnum.PacketType.ExitRoom] = HandleExitRoomAsync,
                [AppEnum.PacketType.SendMessage] = HandleSendMessageAsync,
                [AppEnum.PacketType.RoomNotification] = HandleRoomNotificationAsync
            }, cancellationToken)
    {
        mClient = new TcpClient(ipAddress.ToString(), port);
        mSocketContext = new SocketContext(mClient);
    }

    public override async Task StartAsync()
    {
        await HandleClientReadAsync(mSocketContext, mCts.Token);
    }

    private static async Task HandleWriteAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var messageInput = Console.ReadLine();

            if (messageInput == null)
                break;

            if (messageInput.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
            {
                var exitRoomPacket = new Packet<ExitRoom>(new ExitRoom()); 
                await WritePacket(tcpClient.GetStream(), exitRoomPacket, cancellationToken);
                return;
            }
  
            var sendMessagePacket = 
                new Packet<SendMessage>(new SendMessage { Message = messageInput });
            
            await WritePacket(tcpClient.GetStream(), sendMessagePacket, cancellationToken);
        }
    }
    
    #region 클라이언트 선택 메뉴
    public async Task SendLoginAsync()
    {
        var stream = mClient.GetStream();
        
        Console.WriteLine("Enter UserId : ");
        var userIdInput = Console.ReadLine();
        if (ulong.TryParse(userIdInput, out var userId))
        {
            var packet = 
                new Packet<Login>(new Login
                {
                    SessionId = $"{Guid.NewGuid()}",
                    UserId = userId
                });
            
            await stream.WriteAsync(packet.From(), mCts.Token);
        }
        Console.WriteLine("end =============");
    }

    public async Task SendRoomListAsync()
    {
        if (mClient == null)
            throw new InvalidOperationException("Client is not running.");
        
        var stream = mClient.GetStream();
        
        var packet = new Packet<RoomList>(new RoomList());
        await stream.WriteAsync(packet.From(), mCts!.Token);
    }

    public async Task SendCreateRoomAsync()
    {
        if (mClient == null)
            throw new InvalidOperationException("Client is not running.");
        
        var stream = mClient.GetStream();
        
        var packet = new Packet<CreateRoom>(new CreateRoom());
        await stream.WriteAsync(packet.From(), mCts!.Token);
        await HandleWriteAsync(mClient, mCts!.Token);
        
    }

    public async Task SendEnterRoomAsync()
    {
        if (mClient == null)
            throw new InvalidOperationException("Client is not running.");
        
        var stream = mClient.GetStream();
        
        Console.WriteLine("Enter RoomId : ");
        var roomIdInput = Console.ReadLine();
        if (int.TryParse(roomIdInput, out var roomId))
        {
            var packet = new Packet<EnterRoom>(
                new EnterRoom
                {
                    RoomId = roomId
                });
            await stream.WriteAsync(packet.From(), mCts.Token);
            await HandleWriteAsync(mClient, mCts.Token);
        }
    }
    #endregion
    
    #region 패킷 핸들러 함수 모음
    private static async Task HandleExitRoomAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        // TODO 방에서 쫓겨나는게 필요
    }
    
    private static async Task HandleRoomListAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        var payload = new RoomList();
        payload.Parse(socketContext.PayloadBuffer);

        Console.WriteLine("Room List=================");
        foreach (var roomId in payload.RoomIds)
        {
            Console.WriteLine(roomId);
        }
        Console.WriteLine("==========================");
    }
    
    private static async Task HandleSendMessageAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        var payload = new SendMessage();
        payload.Parse(socketContext.PayloadBuffer);
        Console.WriteLine($"Message: {payload.Message}");
    }
    
    private static async Task HandleRoomNotificationAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        var payload = new RoomNotification();
        payload.Parse(socketContext.PayloadBuffer);
        Console.WriteLine($"Message: {payload.Notification}");
    }
    #endregion
}