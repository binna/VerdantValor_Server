using System.Net;
using System.Net.Sockets;
using MemoryPack;
using Protocol.Chat.Frames;
using Protocol.Chat.Payloads;
using Shared.Types;
using Tcp;

namespace ChatServer;

public class ChatSocketServer : NetworkSocket
{
    private readonly string mServerIp;
    
    private readonly TcpListener mListener;
    private readonly SessionManager mSessionManager;
    
    public ChatSocketServer(IPAddress ipAddress, int port, CancellationToken cancellationToken = default)
        : base(cancellationToken)
    {
        PacketHandlers = new Dictionary<EPacket, Func<SocketContext, CancellationToken, Task>>
        {
            [EPacket.Login] = HandleLoginAsync,
            [EPacket.EnterWorld] = HandleEnterWorldAsync,
            [EPacket.SendMessage] = HandleSendMessageAsync,
            [EPacket.Disconnect] = HandleDisconnectAsync,
        };
        
        mListener = new TcpListener(ipAddress, port);
        mListener.Start();

        mSessionManager = new SessionManager();
        
        mServerIp = $"{Dns.GetHostEntry(Dns.GetHostName()).AddressList[1]}:{port}";
        Console.WriteLine($"[info] Chat Server Start - {mServerIp}");
    }
    
    public override async Task StartAsync()
    {
        while (!mCts.Token.IsCancellationRequested)
        {
            var tcpClient = await mListener.AcceptTcpClientAsync(mCts.Token);
            Console.WriteLine($"[info] Client connected - {tcpClient.Client.RemoteEndPoint}");

            mSessionManager.ConnectedClient.TryAdd(tcpClient, 0);
            
            // fire-and-forget
            //  만약 여기서 await하면, 한 클라이언트 통신이 끝날 때까지 기다림
            // TODO  예외는 무시됨, 이부분에 대한 해결책 필요함
            var socketContext = new SocketContext(tcpClient);
            _ = HandleClientReadAsync(socketContext, mCts.Token);
        }
    }

    #region 패킷 핸들러 함수 모음
    private async Task HandleLoginAsync(
        SocketContext socketContext, CancellationToken cancellationToken)
    {
        var payload = MemoryPackSerializer.Deserialize<LoginReq>(socketContext.PayloadBuffer);

        var userSessionInfo = await mSessionManager.GetUserSessionInfoAsync($"{payload.UserId}");

        if (payload.SessionId != userSessionInfo.SessionId)
        {
            await SendResponsePacket<LoginRes>(
                socketContext.Stream, 
                EPacket.Login, 
                EResponseResult.LoginFailed, 
                cancellationToken); 
            return;
        }

        socketContext.SetSession(payload.SessionId, payload.UserId);
        Console.WriteLine($"[info] WebSessionId - {payload.SessionId} / userId - {payload.UserId}");
        
        mSessionManager.ConnectedClient.TryRemove(socketContext.Client, out _);
        mSessionManager.LoginSessions.TryAdd(payload.UserId, socketContext.Session);

        socketContext.IsLogin = true;
        Console.WriteLine("[info] 연결된 유저: " + mSessionManager.LoginSessions.Count);
        
        userSessionInfo.ChatServerIp = mServerIp;
        await mSessionManager.AddUserSessionInfoAsync($"{payload.UserId}", userSessionInfo);

        await SendResponsePacket<LoginRes>(
            socketContext.Stream,
            EPacket.Login, 
            EResponseResult.Success,
            cancellationToken); 
    }
    
    private async Task HandleEnterWorldAsync(
        SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (!socketContext.IsLogin)
        {
            await SendResponsePacket<EnterWorldRes>(
                socketContext.Stream,
                EPacket.EnterWorld, 
                EResponseResult.LoginRequired,
                cancellationToken); 
            return;
        }

        try
        {
            // TODO 서버 처음에 로딩하기 해결 후 수정
            if (mSessionManager.AddWorld("Korea_1", socketContext.Session.UserId))
            {
                socketContext.Session.CurrentWorld = "Korea_1";
                await SendResponsePacket<EnterWorldRes>(
                    socketContext.Stream,
                    EPacket.EnterWorld, 
                    EResponseResult.Success, 
                    cancellationToken); 
                return;
            }
            await SendResponsePacket<EnterWorldRes>(
                socketContext.Stream,
                EPacket.EnterWorld, 
                EResponseResult.AlreadyIn, 
                cancellationToken); 
        }
        catch (KeyNotFoundException e)
        {
            await SendResponsePacket<EnterWorldRes>(
                socketContext.Stream,
                EPacket.EnterWorld, 
                EResponseResult.NoneSelected, 
                cancellationToken); 
        }
    }
    
    private async Task HandleSendMessageAsync(
        SocketContext socketContext, CancellationToken cancellationToken)
    {
        if (!socketContext.IsLogin)
        {
            await SendResponsePacket<SendMessageRes>(
                socketContext.Stream,
                EPacket.SendMessage,
                EResponseResult.LoginRequired,
                cancellationToken);
            return;
        }
        
        var payload = MemoryPackSerializer
            .Deserialize<SendDirectMessageReq>(socketContext.PayloadBuffer);

        switch (payload.Type)
        {
            case MessageType.Direct:
                Console.WriteLine("일대일 메시지");
                break;
            case MessageType.Party:
                Console.WriteLine("파티 메시지");
                // await BroadcastChatMessageAsync(
                //     SessionManager.BroadcastTarget.Party, 
                //     socketContext.Session.CurrentParty, 
                //     packet, 
                //     cancellationToken);
                break;
            case MessageType.World:
                Console.WriteLine("월드 메시지");
                break;
            case MessageType.Unknown:
            default:
                await SendResponsePacket<ReceiveMessage>(
                    socketContext.Stream,
                    EPacket.SendMessage,
                    EResponseResult.SendMessageInvalidTarget,
                    cancellationToken);
                return;
        }
        
        await SendResponsePacket<ReceiveMessage>(
            socketContext.Stream,
            EPacket.SendMessage,
            EResponseResult.Success,
            cancellationToken);
    }
    
    private Task HandleDisconnectAsync(
        SocketContext socketContext, CancellationToken cancellationToken)
    {
        Console.WriteLine("[SERVER] Client disconnected");
        
        if (socketContext.IsLogin)
        {
            var userId = socketContext.Session.UserId;
            
            mSessionManager.LoginSessions.TryRemove(userId, out _);

            if (socketContext.Session.CurrentWorld is not null)
            {
                if (mSessionManager.World.TryGetValue(socketContext.Session.CurrentWorld, out var users))
                    users.TryRemove(userId, out _);
            }

            if (socketContext.Session.CurrentParty is not null)
            {
                if (mSessionManager.Party.TryGetValue(socketContext.Session.CurrentParty, out var users))
                    users.TryRemove(userId, out _);
            }
        }
        else
            mSessionManager.ConnectedClient.TryRemove(socketContext.Client, out _);
        
        socketContext.Session.Disconnect();

        return Task.CompletedTask;
    }
    #endregion
    
    private static async Task SendResponsePacket<T>(
        NetworkStream stream, EPacket type, EResponseResult code, 
        CancellationToken cancellationToken) where T : struct, IPacketBody, IResponsePacket
    {
        var response = new T { Code = (int)code };
        await stream.WriteAsync(new Packet<T>(type, response).PacketBytes, cancellationToken);
    }
    
    private async Task BroadcastChatMessageAsync<T>(
        SessionManager.BroadcastTarget target,
        string name, 
        Packet<T> packet, 
        CancellationToken cancellationToken) where T : struct, IPacketBody
    {
        var groups = target switch
        {
            SessionManager.BroadcastTarget.World => mSessionManager.World,
            SessionManager.BroadcastTarget.Party => mSessionManager.Party,
            _ => null       /* Unknown 타입도 여기에 포함 */
        };

        if (groups is null || !groups.TryGetValue(name, out var users))
            return;
        
        var sendTasks = new List<Task>();

        foreach (var userId in users.Keys)
        {
            if (mSessionManager.LoginSessions.TryGetValue(userId, out var session))
                sendTasks.Add(WritePacket(session.Stream, packet, cancellationToken));
        }
        
        await Task.WhenAll(sendTasks);
    }
    
    
    // private async Task BroadcastRoomNotificationAsync(int roomId, string notificationMessage, CancellationToken cancellationToken)
    // {
    //     if (mRoomSessions.TryGetValue(roomId, out var roomSessions))
    //     {
    //         var response = new RoomNotification { Notification = notificationMessage };
    //         var packet = new Packet<RoomNotification>(EPacket.Notification, response);
    //         
    //         foreach (var client in roomSessions)
    //         {
    //             await WritePacket(client.Value.Stream, packet, cancellationToken);
    //         }
    //     }
    // }
    
    // private async Task DeleteRoomAsync(int roomId, CancellationToken cancellationToken)
    // {
    //     await BroadcastRoomNotificationAsync(
    //         roomId, 
    //         "방이 삭제되어 퇴장되었습니다.", 
    //         cancellationToken);
    //                     
    //     if (mRoomSessions.TryRemove(roomId, out var roomSessions))
    //     {
    //         foreach (var roomSession in roomSessions)
    //         {
    //             roomSession.Value.RoomId = 0;
    //         }
    //     }
    // }
}