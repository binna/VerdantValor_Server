using System.Net;
using System.Net.Sockets;
using Common;
using MemoryPack;
using Protocol.Chat.Frames;
using Protocol.Chat.Payloads;
using Shared.Types;
using Tcp;

namespace ChatServer;

public class ChatSocketServer : NetworkSocket
{
    private readonly Dictionary<MessageType, Func<SocketContext, MessageKind, CancellationToken, Task<bool>>> mSendMessageHandlers;

    private string mServerIp;

    private TcpListener mListener;
    private SessionManager mSessionManager;

    public ChatSocketServer(CancellationTokenSource cts = default)
        : base(cts)
    {
        PacketHandlers = new Dictionary<EPacket, Func<SocketContext, CancellationToken, Task>>
        {
            [EPacket.Login] = HandleLoginAsync,
            [EPacket.EnterWorld] = HandleEnterWorldAsync,
            [EPacket.SendMessage] = HandleSendMessageAsync,
            [EPacket.Disconnect] = HandleDisconnectAsync,
        };

        mSendMessageHandlers = new Dictionary<MessageType, Func<SocketContext, MessageKind, CancellationToken, Task<bool>>>
        {
            [MessageType.Unknown] = HandleUnknown,
            [MessageType.Direct] = HandleDirect,
            [MessageType.World] = HandleGroup,
            [MessageType.Party] = HandleGroup
        };
    }

    public override async Task StartAsync(IPAddress ipAddress, int port)
    {
        mListener = new TcpListener(ipAddress, port);
        mListener.Start();

        mSessionManager = new SessionManager(
            mConfig.Database.Url,
            mConfig.Redis.Host,
            $"{mConfig.Redis.Port}");
        await mSessionManager.Init(mToken);

        mServerIp = $"{(await Dns.GetHostEntryAsync(Dns.GetHostName())).AddressList[1]}:{port}";
        Console.WriteLine($"[Info] Chat Server Start - {mServerIp}");

        // fire-and-forget
        //  의도적으로 await하지 않음
        //  서버가 살아있음을 주기적으로 Redis에 알리기 위한 하트비트 루프이므로
        //  백그라운드에서 주기적으로 실행이 필요함
        _ = mSessionManager.StartHeartbeatLoopAsync(mConfig.Name, mServerIp, mToken);

        // fire-and-forget
        //  의도적으로 await하지 않음
        //  주기적으로 좀비 세션을 찾아서 연결을 끊는 루프이므로
        //  백그라운드에서 주기적으로 실행이 필요함
        _ = StartConnectionCheckAsync(ShareServerConst.ZOMBIE_CHECK_MINUTES);

        await AcceptAsync();
    }

    public override async Task AcceptAsync()
    {
        while (!mToken.IsCancellationRequested)
        {
            var tcpClient = await mListener.AcceptTcpClientAsync(mToken);
            Console.WriteLine($"[Info] Client Connected - {tcpClient.Client.RemoteEndPoint}");

            mSessionManager.ConnectedClient.TryAdd(tcpClient, 0);

            // fire-and-forget
            //  의도적으로 await하지 않음
            //  만약 여기서 await하면 한 클라이언트의 통신이 끝날 때까지
            //  다음 클라이언트를 Accept하지 못함
            var socketContext = new SocketContext(tcpClient);
            _ = HandleClientReadAsync(socketContext, mToken);
        }
    }

    protected override Task DisconnectClientAsync(SocketContext socketContext)
    {
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
        socketContext.IsLogin = false;
        
        return Task.CompletedTask;
    }

    protected override Task CheckSessionsAsync()
    {
        foreach (var client in mSessionManager.ConnectedClient.Keys)
        {
            if (IsSocketAlive(client)) continue;
            
            mSessionManager.ConnectedClient.TryRemove(client, out _);
            client.Close();
        }
        
        foreach (var session in mSessionManager.LoginSessions.Values)
        {
            if (session.IsAlive()) continue;

            if (session.CurrentWorld is not null)
            {
                if (mSessionManager.World.TryGetValue(session.CurrentWorld, out var world))
                    world.TryRemove(session.UserId, out _);
            }

            if (session.CurrentParty is not null)
            {
                if (mSessionManager.Party.TryGetValue(session.CurrentParty, out var party))
                    party.TryRemove(session.UserId, out _);
            }

            mSessionManager.LoginSessions.TryRemove(session.UserId, out _);
            
            session.Disconnect();
        }
        
        return Task.CompletedTask;
    }

    #region 패킷 핸들러 함수 모음
    private async Task HandleLoginAsync(
        SocketContext socketContext, 
        CancellationToken token)
    {
        var payload = MemoryPackSerializer
            .Deserialize<LoginReq>(socketContext.PayloadBuffer);

        var userSessionInfo = await mSessionManager.GetUserSessionInfoAsync($"{payload.UserId}");

        if (payload.SessionId != userSessionInfo.SessionId)
        {
            await SendResponsePacket<LoginRes>(
                socketContext.Stream,
                EPacket.Login,
                EResponseResult.LoginFailed,
                token);
            return;
        }

        socketContext.SetSession(payload.SessionId, payload.UserId);
        Console.WriteLine($"[Info] WebSessionId - {payload.SessionId} / UserId - {payload.UserId}");

        mSessionManager.ConnectedClient.TryRemove(socketContext.Client, out _);
        mSessionManager.LoginSessions.TryAdd(payload.UserId, socketContext.Session);

        socketContext.IsLogin = true;
        Console.WriteLine("[Info] Total Connected User - " + mSessionManager.LoginSessions.Count);

        userSessionInfo.ChatServerIp = mServerIp;
        await mSessionManager.AddUserSessionInfoAsync($"{payload.UserId}", userSessionInfo);

        await SendResponsePacket<LoginRes>(
            socketContext.Stream,
            EPacket.Login,
            EResponseResult.Success,
            token);
    }

    private async Task HandleEnterWorldAsync(
        SocketContext socketContext, CancellationToken token)
    {
        if (!socketContext.IsLogin)
        {
            await SendResponsePacket<EnterWorldRes>(
                socketContext.Stream,
                EPacket.EnterWorld,
                EResponseResult.LoginRequired,
                token);
            return;
        }
        
        var payload = MemoryPackSerializer
            .Deserialize<EnterWorldReq>(socketContext.PayloadBuffer);

        try
        {
            if (mSessionManager.AddUserToWorld(payload.WorldName, socketContext.Session.UserId))
            {
                socketContext.Session.CurrentWorld = payload.WorldName;
                await SendResponsePacket<EnterWorldRes>(
                    socketContext.Stream,
                    EPacket.EnterWorld,
                    EResponseResult.Success,
                    token);
                return;
            }

            await SendResponsePacket<EnterWorldRes>(
                socketContext.Stream,
                EPacket.EnterWorld,
                EResponseResult.AlreadyIn,
                token);
        }
        catch (KeyNotFoundException ex)
        {
            await SendResponsePacket<EnterWorldRes>(
                socketContext.Stream,
                EPacket.EnterWorld,
                EResponseResult.NoneSelected,
                token);
        }
    }

    private async Task HandleSendMessageAsync(
        SocketContext socketContext, CancellationToken token)
    {
        if (!socketContext.IsLogin)
        {
            await SendResponsePacket<SendMessageRes>(
                socketContext.Stream,
                EPacket.SendMessage,
                EResponseResult.LoginRequired,
                token);
            return;
        }

        var kind = MemoryPackSerializer
            .Deserialize<MessageKind>(socketContext.PayloadBuffer);

        var bSuccess = await mSendMessageHandlers[kind.Type](socketContext, kind, token);
        if (bSuccess)
            await SendResponsePacket<SendMessageRes>(
                socketContext.Stream,
                EPacket.SendMessage,
                EResponseResult.Success,
                token);
    }

    private async Task HandleDisconnectAsync(
        SocketContext socketContext, 
        CancellationToken token)
    {
        await SendResponsePacket<DisconnectRes>(
            socketContext.Stream,
            EPacket.Disconnect,
            EResponseResult.Success,
            mToken);
        
        await DisconnectClientAsync(socketContext);
    }
    #endregion

    #region SendMessage 타입별 핸들러
    private async Task<bool> HandleUnknown(
        SocketContext socketContext,
        MessageKind kind,
        CancellationToken token)
    {
        await SendResponsePacket<SendMessageRes>(
            socketContext.Stream,
            EPacket.SendMessage,
            EResponseResult.SendMessageInvalidTarget,
            token);
        return true;
    }

    private async Task<bool> HandleDirect(
        SocketContext socketContext,
        MessageKind kind,
        CancellationToken token)
    {
        var payload = MemoryPackSerializer
            .Deserialize<SendDirectMessageReq>(socketContext.PayloadBuffer);

        if (!mSessionManager.LoginSessions.TryGetValue(payload.ReceiverUserId, out var session))
        {
            // TODO 다이렉트 메시지 저장 기능 필요
            return false;
        }

        if (kind.Category == MessageCategory.Notification)
        {
            await SendResponsePacket<SendMessageRes>(
                socketContext.Stream,
                EPacket.SendMessage,
                EResponseResult.InvalidInput,
                token);
            return false;
        }
                
        await WritePacket(
            session.Stream, 
            new Packet<SendDirectMessageReq>(EPacket.SendMessage, payload),
            token);
        return true;
    }
    
    private async Task<bool> HandleGroup(
        SocketContext socketContext,
        MessageKind kind,
        CancellationToken token)
    {
        var currentGroup = kind.Type switch
        {
            MessageType.World => socketContext.Session.CurrentWorld,
            MessageType.Party => socketContext.Session.CurrentParty,
            _ => null
        };
        
        if (currentGroup is null) 
            return false;

        switch (kind.Category)
        {
            case MessageCategory.Notification:
            {
                var payload = MemoryPackSerializer
                    .Deserialize<Notification>(socketContext.PayloadBuffer);

                await BroadcastPacketToGroupAsync(
                    kind.Type,
                    currentGroup,
                    new Packet<Notification>(EPacket.SendMessage, payload),
                    token);
                break;
            }
            case MessageCategory.Chat:
            {
                var payload = MemoryPackSerializer
                    .Deserialize<SendGroupMessageReq>(socketContext.PayloadBuffer);

                await BroadcastPacketToGroupAsync(
                    kind.Type,
                    currentGroup,
                    new Packet<SendGroupMessageReq>(EPacket.SendMessage, payload),
                    token);
                break;
            }
            default:
                await SendResponsePacket<SendMessageRes>(
                    socketContext.Stream,
                    EPacket.SendMessage,
                    EResponseResult.InvalidInput,
                    token);
                return false;
        }

        return true;
    }
    #endregion

    private static async Task SendResponsePacket<T>(
        NetworkStream stream, 
        EPacket type, 
        EResponseResult code,
        CancellationToken token) where T : struct, IPacketBody, IResponsePacket
    {
        var response = new T { Code = (int)code };
        await stream.WriteAsync(new Packet<T>(type, response).PacketBytes, token);
    }

    private async Task BroadcastPacketToGroupAsync<T>(
        MessageType target,
        string name,
        Packet<T> packet,
        CancellationToken token) where T : struct, IPacketBody
    {
        var groups = target switch
        {
            MessageType.World => mSessionManager.World,
            MessageType.Party => mSessionManager.Party,
            _ => null /* Unknown 타입도 여기에 포함 */
        };

        if (groups is null || !groups.TryGetValue(name, out var users))
            return;

        var sendTasks = new List<Task>();

        foreach (var userId in users.Keys)
        {
            if (mSessionManager.LoginSessions.TryGetValue(userId, out var session))
                sendTasks.Add(WritePacket(session.Stream, packet, token));
        }

        await Task.WhenAll(sendTasks);
    }
}
