using System.Net;
using System.Net.Sockets;
using MemoryPack;
using Protocol.Chat.Frames;
using Protocol.Chat.Payloads;
using Shared.Types;
using Tcp;

namespace ChatServer.Client;

public class ChatSocketClient : NetworkSocket
{
    private readonly TcpClient mClient;
    private readonly SocketContext mSocketContext;
    
    public ChatSocketClient(IPAddress ipAddress, int port, CancellationToken cancellationToken = default) 
        : base(
            new Dictionary<EPacket, Func<SocketContext, CancellationToken, Task>>
            {
                [EPacket.RoomList] = HandleRoomListAsync,
                [EPacket.ExitRoom] = HandleExitRoomAsync,
                [EPacket.SendMessage] = HandleSendMessageAsync,
                [EPacket.RoomNotification] = HandleRoomNotificationAsync
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
                var exitRoomPacket = new Packet<ExitRoomReq>(EPacket.ExitRoom, new ExitRoomReq());
                await WritePacket(tcpClient.GetStream(), exitRoomPacket, cancellationToken);
                return;
            }
  
            var sendMessagePacket = 
                new Packet<SendMessageReq>(EPacket.SendMessage, new SendMessageReq { Message = messageInput });
            
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
                new Packet<LoginReq>(
                    EPacket.Login, 
                    new LoginReq
                    {
                        SessionId = $"{Guid.NewGuid()}",
                        UserId = userId
                    });
            
            await stream.WriteAsync(packet.PacketBytes, mCts.Token);
        }
        Console.WriteLine("end =============");
    }

    public async Task SendRoomListAsync()
    {
        if (mClient == null)
            throw new InvalidOperationException("Client is not running.");
        
        var stream = mClient.GetStream();
        
        var packet = new Packet<RoomListReq>(EPacket.RoomList, new RoomListReq());
        await stream.WriteAsync(packet.PacketBytes, mCts.Token);
    }

    public async Task SendCreateRoomAsync()
    {
        if (mClient == null)
            throw new InvalidOperationException("Client is not running.");
        
        var stream = mClient.GetStream();
        
        var packet = new Packet<CreateRoomReq>(EPacket.CreateRoom, new CreateRoomReq());
        await stream.WriteAsync(packet.PacketBytes, mCts.Token);
        await HandleWriteAsync(mClient, mCts.Token);
        
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
            var packet = new Packet<EnterRoomReq>(
                EPacket.EnterRoom,
                new EnterRoomReq
                {
                    RoomId = roomId
                });
            await stream.WriteAsync(packet.PacketBytes, mCts.Token);
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
        var payload = MemoryPackSerializer.Deserialize<RoomListReq>(socketContext.PayloadBuffer);

        Console.WriteLine("Room List=================");
        foreach (var roomId in payload.RoomIds)
        {
            Console.WriteLine(roomId);
        }
        Console.WriteLine("==========================");
    }
    
    private static async Task HandleSendMessageAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        var payload = MemoryPackSerializer.Deserialize<SendMessageReq>(socketContext.PayloadBuffer);
        Console.WriteLine($"Message: {payload.Message}");
    }
    
    private static async Task HandleRoomNotificationAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        var payload = MemoryPackSerializer.Deserialize<RoomNotificationReq>(socketContext.PayloadBuffer);
        Console.WriteLine($"Message: {payload.Notification}");
    }
    #endregion
}