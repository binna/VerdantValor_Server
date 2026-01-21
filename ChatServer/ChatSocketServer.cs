using System.Collections.Concurrent;
using System.Net;
using SharedLibrary.Protocol.Common.ChatServer;
using SharedLibrary.Protocol.Packet;
using SharedLibrary.Protocol.Packet.ChatServer;
using SharedLibrary.Tcp;

namespace ChatServer;

public class ChatSocketServer : SocketServer
{
    private static ConcurrentDictionary<int, ConcurrentDictionary<ulong, Session>> mRoomSessions = [];
    private static byte[] mRoomIdsPacket = [];
    private static bool mbUpdated;
    
    // TODO 나중에 동기화 고려 필요
    //      타임스템프 + userId 조합으로 string으로 변경 예정
    private static int roomCount = 0;
    
    private static readonly Timer UpdateRoomIdsTimer = 
        new(UpdateRoomIds, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

    public ChatSocketServer(IPAddress ipAddress, int port)
        : base(
            ipAddress, 
            port, 
            new Dictionary<AppEnum.PacketType, Action<SocketContext, CancellationToken>>
            {
                [AppEnum.PacketType.Login] = HandleLoginAsync,
                [AppEnum.PacketType.CreateRoom] = HandleCreateRoomAsync,
                [AppEnum.PacketType.DeleteRoom] = HandleDeleteRoomAsync,
                [AppEnum.PacketType.RoomList] = HandleRoomListAsync,
                [AppEnum.PacketType.EnterRoom] = HandleEnterRoomAsync,
                [AppEnum.PacketType.ExitRoom] = HandleExitRoomAsync,
                [AppEnum.PacketType.SendMessage] = HandleSendMessageAsync,
                [AppEnum.PacketType.Disconnect] = HandleDisconnectAsync,
            })
    { }
    
    public override void Stop()
    {
        base.Stop();
        mRoomSessions.Clear();
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
    
    private static void HandleLoginAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        // TODO 유효성 검사가 필요
        var payload = new Login();
        payload.Parse(socketContext.Payload);
                        
        socketContext.SessionChange(payload.SessionId, payload.UserId, socketContext.TcpClient);
        Console.WriteLine($"sessionId:{payload.SessionId}//userId:{payload.UserId}");

        mConnectedSessions.TryAdd(payload.UserId, socketContext.Session);
        Console.WriteLine("연결된 유저: " + mConnectedSessions.Count);
    }
    
    private static void HandleCreateRoomAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (socketContext.Session == null)
            return;

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

    private static void HandleDeleteRoomAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (socketContext.Session == null || socketContext.Session.RoomId == 0)
            return;

        _ = DeleteRoomAsync(socketContext.Session.RoomId, cancellationToken);
    }

    private static void HandleRoomListAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (socketContext.Session == null)
            return;
                        
        _ = socketContext.Session.Stream.WriteAsync(mRoomIdsPacket, cancellationToken);
    }
    
    private static void HandleEnterRoomAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (socketContext.Session == null)
            return;

        if (socketContext.Session.RoomId == 0)
        {
            var payload = new EnterRoom();
            payload.Parse(socketContext.Payload);

            if (mRoomSessions.TryGetValue(payload.RoomId, out var roomSessions))
            {
                roomSessions.TryAdd(socketContext.Session.UserId, socketContext.Session);
                socketContext.Session.RoomId = payload.RoomId;
                
                _ = BroadcastRoomNotificationAsync(
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
    
    private static void HandleExitRoomAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (socketContext.Session == null || socketContext.Session.RoomId == 0)
            return;

        if (mRoomSessions.TryGetValue(socketContext.Session.RoomId, out var roomSessions))
        {
            roomSessions.TryRemove(socketContext.Session.UserId, out _);
            socketContext.Session.RoomId = 0;
            mbUpdated = true;
            _ = BroadcastRoomNotificationAsync(
                socketContext.Session.RoomId, 
                $"{socketContext.Session.UserId}가 퇴장하셨습니다.", 
                cancellationToken);
            Console.WriteLine($"방 나가기 성공 {socketContext.Session.RoomId}");
        }
    }
    
    private static void HandleSendMessageAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (socketContext.Session == null)
            return;

        if (socketContext.Session.RoomId != 0)
        {
            var payload = new SendMessage();
            payload.Parse(socketContext.Payload);
            Console.WriteLine($"받음 : {payload.Message}");
            
            var packet = new Packet<SendMessage>(socketContext.Header, payload);
            _ = BroadcastChatMessageAsync(socketContext.Session.RoomId, packet, cancellationToken);
        }
    }
    
    private static void HandleDisconnectAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        Console.WriteLine("[SERVER] Client disconnected");
        if (socketContext.Session != null)
        {
            if (mRoomSessions.TryGetValue(socketContext.Session.RoomId, out var roomSessions))
                roomSessions.TryRemove(socketContext.Session.UserId, out _);
            
            mConnectedSessions.TryRemove(socketContext.Session.UserId, out _);
            socketContext.Session.Disconnect();
        }
    }
}