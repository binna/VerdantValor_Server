using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using VerdantValorShared.Common.ChatServer;
using VerdantValorShared.Packet;
using VerdantValorShared.Packet.ChatServer;
using Tcp;

namespace ChatServer;

public class ChatSocketServer : SocketServer
{
    private static ConcurrentDictionary<TcpClient, byte> mConnectedClient = [];
    private static ConcurrentDictionary<ulong, Session> mLoginSessions = [];
    private static ConcurrentDictionary<int, ConcurrentDictionary<ulong, Session>> mRoomSessions = [];

    private static byte[] mRoomIdsPacket = [];
    private static bool mbUpdated;
    
    private readonly TcpListener mListener;
    
    // TODO 비정상 종료에 대한 로직 필요
    //      Timer 함수로 1분에 한번씩 확인하는 식으로 작업 예정
    
    // TODO 나중에 동기화 고려 필요
    //      타임스템프 + userId 조합으로 string으로 변경 예정
    private static int roomCount = 0;
    
    private static readonly Timer UpdateRoomIdsTimer = 
        new(UpdateRoomIds, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

    public ChatSocketServer(IPAddress ipAddress, int port, CancellationToken cancellationToken = default)
        : base(new Dictionary<AppEnum.PacketType, Func<SocketContext, CancellationToken, Task>>
        {
            [AppEnum.PacketType.Login] = HandleLoginAsync,
            [AppEnum.PacketType.CreateRoom] = HandleCreateRoomAsync,
            [AppEnum.PacketType.DeleteRoom] = HandleDeleteRoomAsync,
            [AppEnum.PacketType.RoomList] = HandleRoomListAsync,
            [AppEnum.PacketType.EnterRoom] = HandleEnterRoomAsync,
            [AppEnum.PacketType.ExitRoom] = HandleExitRoomAsync,
            [AppEnum.PacketType.SendMessage] = HandleSendMessageAsync,
            [AppEnum.PacketType.Disconnect] = HandleDisconnectAsync,
        }, cancellationToken)
    {
        mListener = new TcpListener(ipAddress, port);
        mListener.Start();
    }
    
    public override async Task StartAsync()
    {
        while (!mCts.Token.IsCancellationRequested)
        {
            var tcpClient = await mListener.AcceptTcpClientAsync(mCts.Token);
            Console.WriteLine($"[SERVER] Client connected: {tcpClient.Client.RemoteEndPoint}");

            mConnectedClient.TryAdd(tcpClient, 0);
            
            var socketContext = new SocketContext(tcpClient);
            _ = HandleClientReadAsync(socketContext, mCts.Token);
        }
    }

    private static void UpdateRoomIds(object? o)
    {
        if (!mbUpdated)
            return;
        
        Console.WriteLine("변경 방리스트 정보");
        mbUpdated = false;
        
        var roomIds = mRoomSessions.Keys.ToArray();

        if (roomIds.Length == 0)
            return;
        
        var packet = 
            new Packet<RoomList>(new RoomList
            {
                RoomCount = roomIds.Length,
                RoomIds = roomIds
            });
        mRoomIdsPacket = packet.From();
    }

    #region 패킷 핸들러 함수 모음
    private static async Task HandleLoginAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        // TODO 유효성 검사가 필요
        //  레디스 뎐결해서 레디스 세션ID와 pid가 일치하는지 확인하기
        
        var payload = new Login();
        payload.Parse(socketContext.PayloadBuffer);
                        
        socketContext.SetSession(payload.SessionId, payload.UserId);
        Console.WriteLine($"sessionId:{payload.SessionId}//userId:{payload.UserId}");
        
        mConnectedClient.TryRemove(socketContext.Client, out _);
        mLoginSessions.TryAdd(payload.UserId, socketContext.Session);

        socketContext.IsLogin = true;
        Console.WriteLine("연결된 유저: " + mLoginSessions.Count);
    }
    
    private static async Task HandleCreateRoomAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (!socketContext.IsLogin)
        {
            // TODO 패킷 날릴 예정
            return;
        }

        if (socketContext.Session.RoomId == 0)
        {
            var sessions = new ConcurrentDictionary<ulong, Session>();
            sessions.TryAdd(socketContext.Session.UserId, socketContext.Session);

            int newRoomId;
            do
            {
                newRoomId = Interlocked.Increment(ref roomCount);
            } while (!mRoomSessions.TryAdd(newRoomId, sessions));
            socketContext.Session.RoomId = newRoomId;
            mbUpdated = true;
            Console.WriteLine($"방 만들기 성공 {socketContext.Session.RoomId}");
        }
    }

    private static async Task HandleDeleteRoomAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (!socketContext.IsLogin)
        {
            // TODO 패킷 날릴 예정
            return;
        }
        
        if (socketContext.Session.RoomId == 0)
        {
            // TODO 패킷 날릴 예정
            return;
        }

        await DeleteRoomAsync(socketContext.Session.RoomId, cancellationToken);
    }

    private static async Task HandleRoomListAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (!socketContext.IsLogin)
        {
            // TODO 패킷 날릴 예정
            return;
        }
                        
        await socketContext.Session.Stream.WriteAsync(mRoomIdsPacket, cancellationToken);
    }
    
    private static async Task HandleEnterRoomAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (!socketContext.IsLogin)
        {
            // TODO 패킷 날릴 예정
            return;
        }

        if (socketContext.Session.RoomId == 0)
        {
            var payload = new EnterRoom();
            payload.Parse(socketContext.PayloadBuffer);

            if (mRoomSessions.TryGetValue(payload.RoomId, out var roomSessions))
            {
                roomSessions.TryAdd(socketContext.Session.UserId, socketContext.Session);
                socketContext.Session.RoomId = payload.RoomId;
                
                await BroadcastRoomNotificationAsync(
                    socketContext.Session.RoomId, 
                    $"{socketContext.Session.UserId}가 입장하셨습니다.", 
                    cancellationToken);
                Console.WriteLine($"방 들어가기 성공 {socketContext.Session.RoomId}");
            }
            else
            {
                Console.WriteLine($"검색된 방이 없습니다.");
            }
        }
    }
    
    private static async Task HandleExitRoomAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (!socketContext.IsLogin)
        {
            // TODO 패킷 날릴 예정
            return;
        }
        
        if (socketContext.Session.RoomId == 0)
        {
            // TODO 패킷 날릴 예정
            return;
        }

        if (mRoomSessions.TryGetValue(socketContext.Session.RoomId, out var roomSessions))
        {
            roomSessions.TryRemove(socketContext.Session.UserId, out _);
            mbUpdated = true;
            await BroadcastRoomNotificationAsync(
                socketContext.Session.RoomId, 
                $"{socketContext.Session.UserId}가 퇴장하셨습니다.", 
                cancellationToken);
            socketContext.Session.RoomId = 0;
            Console.WriteLine($"방 나가기 성공 {socketContext.Session.RoomId}");
        }
    }
    
    private static async Task HandleSendMessageAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (!socketContext.IsLogin)
        {
            // TODO 패킷 날릴 예정
            return;
        }

        if (socketContext.Session.RoomId != 0)
        {
            var payload = new SendMessage();
            payload.Parse(socketContext.PayloadBuffer);
            Console.WriteLine($"받음 : {payload.Message}");
            
            var packet = new Packet<SendMessage>(socketContext.Header, payload);
            await BroadcastChatMessageAsync(socketContext.Session.RoomId, packet, cancellationToken);
        }
    }
    
    private static async Task HandleDisconnectAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        Console.WriteLine("[SERVER] Client disconnected");
        if (!socketContext.IsLogin)
        {
            if (mRoomSessions.TryGetValue(socketContext.Session.RoomId, out var roomSessions))
                roomSessions.TryRemove(socketContext.Session.UserId, out _);
            
            //mConnectedSessions.TryRemove(socketContext.Session.UserId, out _);
            socketContext.Session.Disconnect();
        }
    }
    #endregion
    
    private static async Task BroadcastChatMessageAsync(int roomId, Packet<SendMessage> packet, CancellationToken cancellationToken)
    {
        if (mRoomSessions.TryGetValue(roomId, out var roomSessions))
        {
            var packetBuffer = packet.From();
            
            foreach (var client in roomSessions)
            {
                Console.WriteLine($"채팅발송:{roomId}//{client.Value.SessionId}//{client.Value.UserId}");
                await client.Value.Stream.WriteAsync(packetBuffer , cancellationToken);
            }
        }
    }
    
    private static async Task BroadcastRoomNotificationAsync(int roomId, string notificationMessage, CancellationToken cancellationToken)
    {
        if (mRoomSessions.TryGetValue(roomId, out var roomSessions))
        {
            var packet = new Packet<RoomNotification>(
                new RoomNotification { Notification = notificationMessage });
            
            var packetBuffer = packet.From();
            
            foreach (var client in roomSessions)
            {
                Console.WriteLine($"공지발송:{client.Value.SessionId}//{client.Value.UserId}");
                await client.Value.Stream.WriteAsync(packetBuffer, cancellationToken);
            }
        }
    }
    
    private static async Task DeleteRoomAsync(int roomId, CancellationToken cancellationToken)
    {
        await BroadcastRoomNotificationAsync(
            roomId, 
            "방이 삭제되어 퇴장되었습니다.", 
            cancellationToken);
                        
        if (mRoomSessions.TryRemove(roomId, out var roomSessions))
        {
            foreach (var roomSession in roomSessions)
            {
                roomSession.Value.RoomId = 0;
            }
            Console.WriteLine($"방 삭제 성공 {roomId}");
        }
    }
}