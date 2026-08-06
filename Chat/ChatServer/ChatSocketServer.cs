using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Ado.Daos;
using Common;
using Common.KeyValueStore;
using MemoryPack;
using Protocol.Chat.Frames;
using Protocol.Chat.Payloads;
using Shared.Types;
using Tcp;

namespace ChatServer;

public class ChatSocketServer : NetworkSocket
{
    private string mServerIp;
    private TcpListener mListener;
    
    private readonly Dictionary<MessageType, Func<SocketContext, MessageKind, CancellationToken,  Task<EResponseResult>>> mSendMessageHandlers;
    private readonly IChatPartyDao mChatPartyDao;
    private readonly ISessionKeyValueStore mSessionKeyValueStore;
    private readonly WorldPartyManager mWorldPartyManager;
    private readonly SessionManager mSessionManager;
    private readonly Config mConfig;
    
    public ChatSocketServer(
        IChatPartyDao chatPartyDao, 
        ISessionKeyValueStore sessionKeyValueStore, 
        WorldPartyManager worldPartyManager,
        SessionManager sessionManager,
        Config config,
        CancellationTokenSource cts = default) : base(cts)
    {
        mChatPartyDao = chatPartyDao;
        mSessionKeyValueStore = sessionKeyValueStore;
        mWorldPartyManager = worldPartyManager;
        mSessionManager = sessionManager;
        mConfig = config;
        
        RegisterPacketHandlers(
            new Dictionary<EPacket, Func<SocketContext, CancellationToken, Task>>
            {
                [EPacket.Login] = HandleLoginAsync,
                [EPacket.EnterWorld] = HandleEnterWorldAsync,
                [EPacket.CreateParty] = HandleCreatePartyAsync,
                [EPacket.DeleteParty] = HandleDeletePartyAsync,
                [EPacket.EnterParty] = HandleEnterPartyAsync,
                [EPacket.ExitParty] = HandleExitPartyAsync,
                [EPacket.SendMessage] = HandleSendMessageAsync,
                [EPacket.Disconnect] = HandleDisconnectAsync,
            });

        mSendMessageHandlers = new Dictionary<MessageType, Func<SocketContext, MessageKind, CancellationToken, Task<EResponseResult>>>
        {
            [MessageType.Direct] = HandleDirect,
            [MessageType.World] = HandleGroup,
            [MessageType.Party] = HandleGroup
        };
    }

    public override async Task StartAsync(IPAddress ipAddress, int port)
    {
        mListener = new TcpListener(ipAddress, port);
        mListener.Start();

        mServerIp = $"{(await Dns.GetHostEntryAsync(Dns.GetHostName())).AddressList[1]}:{port}";
        LogInfo($"Chat Server Start - {mServerIp}");
        
        await mWorldPartyManager.InitAsync();
        LogInfo("Init Complete");

        // fire-and-forget
        //  의도적으로 await하지 않음
        //  서버가 살아있음을 주기적으로 Redis에 알리기 위한 하트비트 루프이므로
        //  백그라운드에서 주기적으로 실행이 필요함
        _ = StartHeartbeatLoopAsync();

        // fire-and-forget
        //  의도적으로 await하지 않음
        //  주기적으로 좀비 세션을 찾아서 연결을 끊는 루프이므로
        //  백그라운드에서 주기적으로 실행이 필요함
        _ = ConnectionCheckLoopAsync(ShareServerConst.ZOMBIE_CHECK_MINUTES);

        await AcceptAsync();
    }

    protected override async Task AcceptAsync()
    {
        while (!mToken.IsCancellationRequested)
        {
            var tcpClient = await mListener.AcceptTcpClientAsync(mToken);
            LogInfo($"Client Connected - {tcpClient.Client.RemoteEndPoint}");

            if (!mSessionManager.AddConnectedClient(tcpClient))
            {
                LogError("Already Connected");
                tcpClient.Close();
                continue;
            }

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

            if (!mSessionManager.RemoveLoginSession(userId))
                LogError($"Failed To Remove Login Session - userId={userId}");

            if (socketContext.Session.CurrentWorld is not null)
            {
                var code = mWorldPartyManager.RemoveUserFromWorld(socketContext.Session.CurrentWorld, userId);
                if (code != EResponseResult.Success)
                    LogError($"Failed To Remove From World - userId={userId}, code={code}");
            }

            if (socketContext.Session.CurrentParty is not null)
            {
                var code = mWorldPartyManager.RemoveUserFromParty(socketContext.Session.CurrentParty, userId);
                if (code != EResponseResult.Success)
                    LogError($"Failed To Remove From Party - userId={userId}, code={code}");
            }

            socketContext.Session.Disconnect();
            return Task.CompletedTask;
        }
        
        if (!mSessionManager.RemoveConnectedClient(socketContext.Client))
            LogError("Failed To Remove Session - No Login");
        
        socketContext.Client.Close();
        return Task.CompletedTask;
    }

    protected override async Task StartHeartbeatLoopAsync()
    {
        while (!mToken.IsCancellationRequested)
        {
            try
            {
                var bSuccess = await mSessionKeyValueStore.RefreshServerHeartbeatAsync(mConfig.Name, mServerIp);
                if (!bSuccess)
                    LogError($"{mConfig.Name} Heartbeat Failed - {mServerIp}");
                
                await Task.Delay(TimeSpan.FromMinutes(ShareServerConst.HEARTBEAT_MINUTES), mToken);
            }
            catch (OperationCanceledException)
            {
                // 취소로 인한 종료는 정상 흐름이므로 루프를 빠져나간다.
                break;
            }
            catch (Exception ex)
            {
                LogError($"Heartbeat Exception: {ex}");
            }
        }
    }

    protected override Task CheckSessionsAsync()
    {
        foreach (var client in mSessionManager.GetConnectedClients())
        {
            if (IsSocketAlive(client)) continue;

            if (!mSessionManager.RemoveConnectedClient(client))
                LogError("Failed To Remove Zombie Client From ConnectedClient");

            client.Close();
        }

        foreach (var session in mSessionManager.GetLoginSessions())
        {
            if (session.IsAlive()) continue;

            if (session.CurrentWorld is not null)
            {
                var code = mWorldPartyManager.RemoveUserFromWorld(session.CurrentWorld, session.UserId);
                if (code != EResponseResult.Success)
                    LogError($"Failed To Remove Zombie Session From World: WorldName={session.CurrentWorld}, UserId={session.UserId}, Code={code}");
            }

            if (session.CurrentParty is not null)
            {
                var code = mWorldPartyManager.RemoveUserFromParty(session.CurrentParty, session.UserId);
                if (code != EResponseResult.Success)
                    LogError($"Failed To Remove Zombie Session From Party: PartyId={session.CurrentParty}, UserId={session.UserId}, Code={code}");
            }

            if (!mSessionManager.RemoveLoginSession(session.UserId))
                LogError($"Failed To Remove Zombie Session From LoginSessions: UserId={session.UserId}");

            session.Disconnect();
        }

        return Task.CompletedTask;
    }

    #region 패킷 핸들러 함수 모음
    private async Task HandleLoginAsync(SocketContext socketContext, CancellationToken token)
    {
        var payload = MemoryPackSerializer.Deserialize<LoginReq>(socketContext.PayloadBuffer);
        var userSessionInfo = await mSessionKeyValueStore.GetUserSessionInfoAsync($"{payload.UserId}");

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
        userSessionInfo.ChatServerIp = mServerIp;
        
        var code = await mSessionManager.LoginAsync(socketContext, userSessionInfo);

        if (code == EResponseResult.Success)
        {
            socketContext.IsLogin = true;
            LogInfo($"Login Success - WebSessionId: {payload.SessionId} / UserId: {payload.UserId}");
        }
        
        await SendResponsePacket<LoginRes>(
            socketContext.Stream,
            EPacket.Login,
            code,
            token);
    }

    private async Task HandleEnterWorldAsync(SocketContext socketContext, CancellationToken token)
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
        
        var payload = MemoryPackSerializer.Deserialize<EnterWorldReq>(socketContext.PayloadBuffer);
        
        var code = mWorldPartyManager.AddUserToWorld(payload.WorldName, socketContext.Session.UserId);
        await SendResponsePacket<EnterWorldRes>(
            socketContext.Stream,
            EPacket.EnterWorld,
            code,
            token);
    }

    private async Task HandleCreatePartyAsync(SocketContext socketContext, CancellationToken token)
    {
        var payload = MemoryPackSerializer.Deserialize<CreatePartyReq>(socketContext.PayloadBuffer);

        var code = await mWorldPartyManager.CreatePartyAsync(payload.PartyId);

        await SendResponsePacket<CreatePartyRes>(
            socketContext.Stream,
            EPacket.CreateParty,
            code,
            token);
    }

    private async Task HandleDeletePartyAsync(SocketContext socketContext, CancellationToken token)
    {
        var payload = MemoryPackSerializer.Deserialize<DeletePartyReq>(socketContext.PayloadBuffer);

        var code = await mWorldPartyManager.DeletePartyAsync(payload.PartyId);

        await SendResponsePacket<DeletePartyRes>(
            socketContext.Stream,
            EPacket.DeleteParty,
            code,
            token);
    }

    private async Task HandleEnterPartyAsync(SocketContext socketContext, CancellationToken token)
    {
        if (!socketContext.IsLogin)
        {
            await SendResponsePacket<EnterPartyRes>(
                socketContext.Stream,
                EPacket.EnterParty,
                EResponseResult.LoginRequired,
                token);
            return;
        }

        var (code, partyId) = await mWorldPartyManager.AddUserToPartyAsync(socketContext.Session.UserId);
        
        if (code == EResponseResult.Success)
            socketContext.Session.CurrentParty = partyId;
        
        await SendResponsePacket<EnterPartyRes>(
            socketContext.Stream,
            EPacket.EnterParty,
            code,
            token);
    }

    private async Task HandleExitPartyAsync(SocketContext socketContext, CancellationToken token)
    {
        if (!socketContext.IsLogin)
        {
            await SendResponsePacket<ExitPartyRes>(
                socketContext.Stream,
                EPacket.ExitParty,
                EResponseResult.LoginRequired,
                token);
            return;
        }
        
        var userId = socketContext.Session.UserId;
        var partyId = socketContext.Session.CurrentParty;
        if (partyId is null)
        {
            await SendResponsePacket<ExitPartyRes>(
                socketContext.Stream,
                EPacket.ExitParty,
                EResponseResult.NotIn,
                token);
            return;
        }

        var code = mWorldPartyManager.RemoveUserFromParty(partyId, userId);
        if (code == EResponseResult.Success)
        {
            var notification = new Notification
            {
                Content = $"{userId}님이 파티를 나갔습니다."
            };

            socketContext.Session.CurrentParty = null;
            await BroadcastPacketToGroupAsync(
                MessageType.Party,
                partyId,
                new Packet<Notification>(EPacket.SendMessage, notification),
                token);
        }

        await SendResponsePacket<ExitPartyRes>(
            socketContext.Stream,
            EPacket.ExitParty,
            code,
            token);
    }

    private async Task HandleSendMessageAsync(SocketContext socketContext, CancellationToken token)
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

        var kind = MemoryPackSerializer.Deserialize<MessageKind>(socketContext.PayloadBuffer);

        if (!mSendMessageHandlers.TryGetValue(kind.Type, out var handler))
        {
            await SendResponsePacket<SendMessageRes>(
                socketContext.Stream,
                EPacket.SendMessage,
                EResponseResult.InvalidInput,
                token);
            return;
        }

        var code = await handler(socketContext, kind, token);
        await SendResponsePacket<SendMessageRes>(
            socketContext.Stream,
            EPacket.SendMessage,
            code,
            token);
    }

    private async Task HandleDisconnectAsync(SocketContext socketContext, CancellationToken token)
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
    private async Task<EResponseResult> HandleDirect(SocketContext socketContext, MessageKind kind, CancellationToken token)
    {
        var payload = MemoryPackSerializer.Deserialize<SendDirectMessageReq>(socketContext.PayloadBuffer);

        // TODO 멀티일때, 어떤식으로 다른 서버에 통신을 할지 고민 필요
        if (!mSessionManager.TryGetLoginSession(payload.ReceiverUserId, out var session))
            return EResponseResult.ReceiverOffline;

        if (session is null)
            return EResponseResult.ReceiverOffline;

        if (kind.Category == MessageCategory.Notification)
            return EResponseResult.InvalidInput;
                
        await WritePacket(
            session.Stream, 
            new Packet<SendDirectMessageReq>(EPacket.SendMessage, payload),
            token);
        return EResponseResult.Success;
    }
    
    private async Task<EResponseResult>  HandleGroup(SocketContext socketContext, MessageKind kind, CancellationToken token)
    {
        var currentGroup = kind.Type switch
        {
            MessageType.World => socketContext.Session.CurrentWorld,
            MessageType.Party => socketContext.Session.CurrentParty,
            _ => null
        };
        
        if (currentGroup is null) 
            return EResponseResult.NotIn;

        switch (kind.Category)
        {
            case MessageCategory.Notification:
            {
                var payload = MemoryPackSerializer.Deserialize<Notification>(socketContext.PayloadBuffer);
                await BroadcastPacketToGroupAsync(
                    kind.Type,
                    currentGroup,
                    new Packet<Notification>(EPacket.SendMessage, payload),
                    token);
                break;
            }
            case MessageCategory.Chat:
            {
                var payload = MemoryPackSerializer.Deserialize<SendGroupMessageReq>(socketContext.PayloadBuffer);
                await BroadcastPacketToGroupAsync(
                    kind.Type,
                    currentGroup,
                    new Packet<SendGroupMessageReq>(EPacket.SendMessage, payload),
                    token);
                break;
            }
            default:
                return EResponseResult.InvalidInput;
        }

        return EResponseResult.Success;
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
        MessageType type,
        string name,
        Packet<T> packet,
        CancellationToken token) where T : struct, IPacketBody
    {
        ConcurrentDictionary<ulong, byte>? users = null;

        switch (type)
        {
            case MessageType.World:
                mWorldPartyManager.TryGetWorldMembers(name, out users);
                break;
            case MessageType.Party:
                mWorldPartyManager.TryGetPartyMembers(name, out users);
                break;
        }

        if (users is null)
            return;

        var sendTasks = new List<Task>();

        foreach (var userId in users.Keys)
        {
            if (mSessionManager.TryGetLoginSession(userId, out var session))
            {
                if (session is null)
                    continue;
                
                sendTasks.Add(WritePacket(session.Stream, packet, token));
            }
        }

        await Task.WhenAll(sendTasks);
    }
}
