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
    // TODO RequestId를 혹시 몰라 추가했음
    //  반드시 req, res 1:1 관계이어야 한다면 필요할 것 같아서
    //  만약 필요없다면 패킷 구조 자체를 수정해야함
    
    // TODO 지금은 switch문으로 했지만
    //  추후 게임 데이터 기반으로 Table 만들어서 Message 뽑아내는 방식으로 바꿀 예정
    
    // TODO 웹 서버 부분도 isSuccess, Message 제거하고 게임 데이터 Table 기반으로 할지도 고민해보기
    //  이렇게되면 웹서버 Response 용량을 절약할 수 있어 최종적으로 패킷 절약으로 비용 절감을 노릴 수 있다고 생각
    
    private readonly TcpClient mClient;
    private readonly SocketContext mSocketContext;
    
    public ChatSocketClient(IPAddress ipAddress, int port, CancellationToken cancellationToken = default) 
        : base(
            new Dictionary<EPacket, Func<SocketContext, CancellationToken, Task>>
            {
                [EPacket.Login] = HandleLoginAsync,
                [EPacket.CreateRoom] = HandleCreateRoomAsync,
                [EPacket.RoomList] = HandleRoomListAsync,
                [EPacket.EnterRoom] = HandleEnterRoomAsync,
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
    
            if (string.IsNullOrEmpty(messageInput))
                continue;
    
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
    public async Task SendLoginReqAsync(ulong userId)
    {
        mSocketContext.SetSession($"{Guid.NewGuid()}", userId);
        
        var packet = 
            new Packet<LoginReq>(
                EPacket.Login, 
                new LoginReq
                {
                    SessionId = mSocketContext.Session.SessionId,
                    UserId = userId
                });
        await mSocketContext.Stream.WriteAsync(packet.PacketBytes, mCts.Token);
    }

    public async Task SendRoomListReqAsync()
    {
        var packet = new Packet<RoomListReq>(EPacket.RoomList, new RoomListReq());
        await mSocketContext.Stream.WriteAsync(packet.PacketBytes, mCts.Token);
    }

    public async Task SendCreateRoomAsync()
    {
        var packet = new Packet<CreateRoomReq>(EPacket.CreateRoom, new CreateRoomReq());
        await mSocketContext.Stream.WriteAsync(packet.PacketBytes, mCts.Token);
        await HandleWriteAsync(mClient, mCts.Token);
    }

    public async Task SendEnterRoomAsync(int roomId)
    {
        var packet = new Packet<EnterRoomReq>(
            EPacket.EnterRoom,
            new EnterRoomReq
            {
                RoomId = roomId
            });
        await mSocketContext.Stream.WriteAsync(packet.PacketBytes, mCts.Token);
        await HandleWriteAsync(mClient, mCts.Token);
    }
    #endregion
    
    #region 패킷 핸들러 함수 모음
    private static async Task HandleLoginAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        var payload = MemoryPackSerializer.Deserialize<LoginRes>(socketContext.PayloadBuffer);

        switch ((EResponseResult)payload.Code)
        {
            case EResponseResult.Success:
                Console.WriteLine("[Notice] 로그인 성공");
                socketContext.IsLogin = true;
                break;
            default:
                Console.WriteLine("[Notice] 로그인 실패");
                Console.WriteLine($"[Notice] 처리되지 않음 {payload.Code}");
                socketContext.IsLogin = false;
                break;
        }
    }

    private static async Task HandleCreateRoomAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        var payload = MemoryPackSerializer.Deserialize<CreateRoomRes>(socketContext.PayloadBuffer);

        switch ((EResponseResult)payload.Code)
        {
            case EResponseResult.Success:
                Console.WriteLine("[Notice] 방 생성 성공, 생성한 방에 입장");
                break;
            case EResponseResult.LoginRequired:
                Console.WriteLine("[Notice] 로그인 필요");
                break;
            case EResponseResult.AlreadyInRoom:
                Console.WriteLine("[Notice] 이미 소속된 방 있음");
                break;
            default:
                Console.WriteLine($"[Notice] 처리되지 않음 {payload.Code}");
                break;
        }
    }
    
    // TODO Delete Room
    
    
    private static async Task HandleRoomListAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        var payload = MemoryPackSerializer.Deserialize<RoomListRes>(socketContext.PayloadBuffer);
        
        switch ((EResponseResult)payload.Code)
        {
            case EResponseResult.Success:
                Console.WriteLine("[Notice] Show Room List===");
                foreach (var roomId in payload.RoomIds)
                {
                    Console.WriteLine(roomId);
                }
                Console.WriteLine("==========================");
                break;
            case EResponseResult.LoginRequired:
                Console.WriteLine("[Notice] 로그인 필요");
                break;
            default:
                Console.WriteLine($"[Notice] 처리되지 않음 {payload.Code}");
                break;
        }
    }
    
    private static async Task HandleEnterRoomAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        var payload = MemoryPackSerializer.Deserialize<EnterRoomRes>(socketContext.PayloadBuffer);
        
        switch ((EResponseResult)payload.Code)
        {
            case EResponseResult.Success:
                Console.WriteLine("[Notice] 방에 들어왔습니다.");
                break;
            case EResponseResult.LoginRequired:
                Console.WriteLine("[Notice] 로그인 필요");
                break;
            case EResponseResult.NoRoomSelected:
                Console.WriteLine("검색된 방이 없습니다.");
                break;
            case EResponseResult.AlreadyInRoom:
                Console.WriteLine("[Notice] 이미 방 안에 들어가 있습니다..");
                break;
            default:
                Console.WriteLine($"[Notice] 처리되지 않음 {payload.Code}");
                break;
        }
    }

    private static async Task HandleExitRoomAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        var payload = MemoryPackSerializer.Deserialize<ExitRoomRes>(socketContext.PayloadBuffer);
        
        switch ((EResponseResult)payload.Code)
        {
            case EResponseResult.Success:
                Console.WriteLine("[Notice] 방을 정상적으로 나갔습니다.");
                break;
            case EResponseResult.LoginRequired:
                Console.WriteLine("[Notice] 로그인 필요");
                break;
            case EResponseResult.AlreadyOutOfRoom:
                Console.WriteLine("[Notice] 현재 해당 방에 참여 중이 아닙니다.");
                break;
            default:
                Console.WriteLine($"[Notice] 처리되지 않음 {payload.Code}");
                break;
        }
    }
    
    private static async Task HandleSendMessageAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        var payload = MemoryPackSerializer.Deserialize<SendMessageRes>(socketContext.PayloadBuffer);
        Console.WriteLine($"|유저 {payload.userId} 대화||||{payload.Message}");
    }
    
    private static async Task HandleRoomNotificationAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        var payload = MemoryPackSerializer.Deserialize<RoomNotification>(socketContext.PayloadBuffer);
        Console.WriteLine($"*공지사항* {payload.Notification}");
    }
    #endregion
}