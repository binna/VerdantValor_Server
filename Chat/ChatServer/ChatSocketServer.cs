using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Common.KeyValueStore;
using MemoryPack;
using Protocol.Chat.Frames;
using Protocol.Chat.Payloads;
using Redis;
using Shared.Types;
using Tcp;

namespace ChatServer;

public class ChatSocketServer : NetworkSocket
{
    // TODO 나중에 세션 관리하는 매니저를 만들 예정
    //  Manager는 상태와 객체를 관리하고, Service는 비즈니스 로직을 수행한다.
    //  Manager = 관리한다 (Manage)
    //  Service = 일을 한다 (Do)
    
    // TODO 연결된 세션, 그리고 로그인 성공한 세션 나누기
    //  연결된 세션은 로그인을 뭘하든지 리스폰 날리기
    
    private ConcurrentDictionary<string, byte> mSessionIdSet = [];
    private ConcurrentDictionary<TcpClient, byte> mConnectedClient = [];
    private ConcurrentDictionary<ulong, Session> mLoginSessions = [];
    private static ConcurrentDictionary<int, ConcurrentDictionary<ulong, Session>> mRoomSessions = [];
    private ISessionKeyValueStore mSessionKeyValueStore = new SessionKeyValueStore(
        new RedisCacheDriver("localhost", $"{6379}", 2));

    private static Packet<RoomListRes> mRoomListResPacket = new(
        EPacket.RoomList,
        new RoomListRes
        {
            Code = (int)EResponseResult.Success,
            RoomCount = 0,
            RoomIds = []
        });

    private static bool mbUpdated;
    
    private readonly string mMyIp;
    private readonly TcpListener mListener;
    private readonly ISessionKeyValueStore sessionKeyValueStore;
    
    // TODO 비정상 종료에 대한 로직 필요
    //      Timer 함수로 1분에 한번씩 확인하는 식으로 작업 예정
    
    // TODO 나중에 동기화 고려 필요
    //      타임스템프 + userId 조합으로 string으로 변경 예정
    
    private int roomCount = 0;
    
    private readonly Timer UpdateRoomIdsTimer = 
        new(UpdateRoomIds, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

    public ChatSocketServer(IPAddress ipAddress, int port, CancellationToken cancellationToken = default)
        : base(cancellationToken)
    {
        PacketHandlers = new Dictionary<EPacket, Func<SocketContext, CancellationToken, Task>>
        {
            [EPacket.Login] = HandleLoginAsync,
            [EPacket.CreateRoom] = HandleCreateRoomAsync,
            [EPacket.DeleteRoom] = HandleDeleteRoomAsync,
            [EPacket.RoomList] = HandleRoomListAsync,
            [EPacket.EnterRoom] = HandleEnterRoomAsync,
            [EPacket.ExitRoom] = HandleExitRoomAsync,
            [EPacket.SendMessage] = HandleSendMessageAsync,
            [EPacket.Disconnect] = HandleDisconnectAsync,
        };
        
        mListener = new TcpListener(ipAddress, port);
        mListener.Start();
        
        sessionKeyValueStore = 
            new SessionKeyValueStore(
                new RedisCacheDriver("localhost", $"{6379}", 2));
        
        mMyIp = $"{Dns.GetHostEntry(Dns.GetHostName()).AddressList[1]}:{port}";
        Console.WriteLine($"Chat Server Start : {mMyIp}");
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

        var payload = new RoomListRes
        {
            Code = (int)EResponseResult.Success,
            RoomCount = roomIds.Length,
            RoomIds = roomIds
        };
        mRoomListResPacket = new Packet<RoomListRes>(EPacket.RoomList, payload);
    }
    
    private static Packet<T> CreateResponsePacket<T>(EPacket type, EResponseResult code) where T : struct, IPacketBody, IResponsePacket
    {
        var response = new T { Code = (int)code };
        return new Packet<T>(type, response);
    }

    #region 패킷 핸들러 함수 모음
    private async Task HandleLoginAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        // TODO 결국 웹서버 세션 ID가이 키 같은 느낌
        //      웹 서버의 유효기간에 맞춰서 TTL 설정하기
        var payload = MemoryPackSerializer.Deserialize<LoginReq>(socketContext.PayloadBuffer);

        var data = 
            await mSessionKeyValueStore.GetUserSessionInfoAsync($"{payload.UserId}");

        if (payload.SessionId != data.SessionId)
        {
            var packet = CreateResponsePacket<CreateRoomRes>(EPacket.Login, EResponseResult.LoginFailed); 
            await WritePacket(socketContext.Stream, packet, cancellationToken);
            return;
        }

        socketContext.SetSession(payload.SessionId, payload.UserId);
        Console.WriteLine($"WebSessionId:{payload.SessionId}//userId:{payload.UserId}");
        
        mConnectedClient.TryRemove(socketContext.Client, out _);
        mLoginSessions.TryAdd(payload.UserId, socketContext.Session);

        socketContext.IsLogin = true;
        Console.WriteLine("연결된 유저: " + mLoginSessions.Count);
        
        data.ChatServerIp = mMyIp;
        await mSessionKeyValueStore.AddUserSessionInfoAsync($"{payload.UserId}", data);

        {
            var packet = CreateResponsePacket<CreateRoomRes>(EPacket.Login, EResponseResult.Success); 
            await WritePacket(socketContext.Stream, packet, cancellationToken);
        }
    }
    
    private async Task HandleCreateRoomAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (!socketContext.IsLogin)
        {
            var response = new CreateRoomRes { Code = (int)EResponseResult.LoginRequired };
            var packet = new Packet<CreateRoomRes>(EPacket.CreateRoom, response);
            await WritePacket(socketContext.Stream, packet, cancellationToken);
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
            Console.WriteLine($"[Notice] 방 만들기 성공({socketContext.Session.RoomId})");
            
            var response = new CreateRoomRes { Code = (int)EResponseResult.Success };
            var packet = new Packet<CreateRoomRes>(EPacket.CreateRoom, response);
            await WritePacket(socketContext.Stream, packet, cancellationToken);
            return;
        }

        {
            var response = new CreateRoomRes { Code = (int)EResponseResult.AlreadyInRoom };
            var packet = new Packet<CreateRoomRes>(EPacket.CreateRoom, response);
            await WritePacket(socketContext.Stream, packet, cancellationToken);
        }
    }

    // TODO 삭제랑 로그아웃 둘다 작업 필요
    private async Task HandleDeleteRoomAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        LoginRes response;
        
        if (!socketContext.IsLogin)
            response = new LoginRes { Code = (int)EResponseResult.LoginRequired };
        
        else if (socketContext.Session.RoomId == 0)
            response = new LoginRes { Code = (int)EResponseResult.AlreadyOutOfRoom };

        else
        {
            response = new LoginRes { Code = (int)EResponseResult.Success };
            await DeleteRoomAsync(socketContext.Session.RoomId, cancellationToken);
        }
        
        var packet = new Packet<LoginRes>(EPacket.Login, response);
        await WritePacket(socketContext.Stream, packet, cancellationToken);
    }

    private async Task HandleRoomListAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (!socketContext.IsLogin)
        {
            var response = new RoomListRes { Code = (int)EResponseResult.LoginRequired };
            var packet = new Packet<RoomListRes>(EPacket.CreateRoom, response);
            await WritePacket(socketContext.Stream, packet, cancellationToken);
            return;
        }

        {
            var packet = mRoomListResPacket;
            await WritePacket(socketContext.Stream, packet, cancellationToken);
        }
    }
    
    private async Task HandleEnterRoomAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (!socketContext.IsLogin)
        {
            var response = new EnterRoomRes { Code = (int)EResponseResult.LoginRequired };
            var packet = new Packet<EnterRoomRes>(EPacket.EnterRoom, response);
            await WritePacket(socketContext.Stream, packet, cancellationToken);
            return;
        }

        if (socketContext.Session.RoomId == 0)
        {
            var payload = MemoryPackSerializer.Deserialize<EnterRoomReq>(socketContext.PayloadBuffer);

            if (mRoomSessions.TryGetValue(payload.RoomId, out var roomSessions))
            {
                roomSessions.TryAdd(socketContext.Session.UserId, socketContext.Session);
                socketContext.Session.RoomId = payload.RoomId;
                
                var response = new EnterRoomRes { Code = (int)EResponseResult.Success };
                var packet = new Packet<EnterRoomRes>(EPacket.EnterRoom, response);
                await WritePacket(socketContext.Stream, packet, cancellationToken);
                
                await BroadcastRoomNotificationAsync(
                    socketContext.Session.RoomId, 
                    $"{socketContext.Session.UserId}가 입장하셨습니다.", 
                    cancellationToken);
                return;
            }
            
            {
                var response = new EnterRoomRes { Code = (int)EResponseResult.NoRoomSelected };
                var packet = new Packet<EnterRoomRes>(EPacket.EnterRoom, response);
                await WritePacket(socketContext.Stream, packet, cancellationToken);
                return;
            }
        }

        {
            var response = new EnterRoomRes { Code = (int)EResponseResult.AlreadyInRoom };
            var packet = new Packet<EnterRoomRes>(EPacket.EnterRoom, response);
            await WritePacket(socketContext.Stream, packet, cancellationToken);
        }
    }
    
    private async Task HandleExitRoomAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (!socketContext.IsLogin)
        {
            var response = new ExitRoomRes { Code = (int)EResponseResult.LoginRequired };
            var packet = new Packet<ExitRoomRes>(EPacket.ExitRoom, response);
            await WritePacket(socketContext.Stream, packet, cancellationToken);
            return;
        }
        
        if (socketContext.Session.RoomId == 0)
        {
            var response = new ExitRoomRes { Code = (int)EResponseResult.AlreadyOutOfRoom };
            var packet = new Packet<ExitRoomRes>(EPacket.ExitRoom, response);
            await WritePacket(socketContext.Stream, packet, cancellationToken);
            return;
        }

        if (mRoomSessions.TryGetValue(socketContext.Session.RoomId, out var roomSessions))
        {
            roomSessions.TryRemove(socketContext.Session.UserId, out _);
            mbUpdated = true;
            
            var response = new ExitRoomRes { Code = (int)EResponseResult.Success };
            var packet = new Packet<ExitRoomRes>(EPacket.ExitRoom, response);
            await WritePacket(socketContext.Stream, packet, cancellationToken);
            
            await BroadcastRoomNotificationAsync(
                socketContext.Session.RoomId, 
                $"{socketContext.Session.UserId}가 퇴장하셨습니다.", 
                cancellationToken);
            socketContext.Session.RoomId = 0;
        }
    }
    
    private  async Task HandleSendMessageAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (!socketContext.IsLogin)
        {
            var response = new SendMessageRes { Code = (int)EResponseResult.LoginRequired };
            var packet = new Packet<SendMessageRes>(EPacket.SendMessage, response);
            await WritePacket(socketContext.Stream, packet, cancellationToken);
            return;
        }
        
        if (socketContext.Session.RoomId == 0)
        {
            var response = new SendMessageRes { Code = (int)EResponseResult.NotInRoom };
            var packet = new Packet<SendMessageRes>(EPacket.SendMessage, response);
            await WritePacket(socketContext.Stream, packet, cancellationToken);
            return;
        }

        {
            var payload = MemoryPackSerializer.Deserialize<SendMessageReq>(socketContext.PayloadBuffer);
            var response = new SendMessageRes
            {
                Code = (int)EResponseResult.Success, 
                userId = socketContext.Session.UserId, 
                Message = payload.Message
            };
            var packet = new Packet<SendMessageRes>(EPacket.SendMessage, response);
            await BroadcastChatMessageAsync(socketContext.Session.RoomId, packet, cancellationToken);
        }
    }
    
    private async Task HandleDisconnectAsync(SocketContext socketContext, CancellationToken cancellationToken)
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
    
    private async Task BroadcastChatMessageAsync(int roomId, Packet<SendMessageRes> packet, CancellationToken cancellationToken)
    {
        if (mRoomSessions.TryGetValue(roomId, out var roomSessions))
        {
            foreach (var client in roomSessions)
            {
                await WritePacket(client.Value.Stream, packet, cancellationToken); 
            }
        }
    }
    
    private async Task BroadcastRoomNotificationAsync(int roomId, string notificationMessage, CancellationToken cancellationToken)
    {
        if (mRoomSessions.TryGetValue(roomId, out var roomSessions))
        {
            var response = new RoomNotification { Notification = notificationMessage };
            var packet = new Packet<RoomNotification>(EPacket.RoomNotification, response);
            
            foreach (var client in roomSessions)
            {
                await WritePacket(client.Value.Stream, packet, cancellationToken);
            }
        }
    }
    
    private async Task DeleteRoomAsync(int roomId, CancellationToken cancellationToken)
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
        }
    }
}