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
    private string mServerIp;

    private TcpListener mListener;
    private SessionManager mSessionManager;

    public ChatSocketServer(CancellationToken cancellationToken = default)
        : base(cancellationToken)
    {
        PacketHandlers = new Dictionary<EPacket, Func<SocketContext, CancellationToken, Task>>
        {
            [EPacket.Login] = HandleLoginAsync,
            [EPacket.EnterWorld] = HandleEnterWorldAsync,
            [EPacket.SendMessage] = HandleSendMessageAsync,
            [EPacket.Disconnect] = HandleDisconnectAsync,
        };
    }

    public override async Task StartAsync(IPAddress ipAddress, int port)
    {
        mListener = new TcpListener(ipAddress, port);
        mListener.Start();

        mSessionManager = new SessionManager();

        mServerIp = $"{Dns.GetHostEntry(Dns.GetHostName()).AddressList[1]}:{port}";
        Console.WriteLine($"[info] Chat Server Start - {mServerIp}");

        await AcceptAsync();
    }

    public override async Task AcceptAsync()
    {
        while (!mCts.Token.IsCancellationRequested)
        {
            var tcpClient = await mListener.AcceptTcpClientAsync(mCts.Token);
            Console.WriteLine($"[info] Client connected - {tcpClient.Client.RemoteEndPoint}");

            mSessionManager.ConnectedClient.TryAdd(tcpClient, 0);

            // fire-and-forget
            //  의도적으로 await하지 않음
            //  만약 여기서 await하면 한 클라이언트의 통신이 끝날 때까지
            //  다음 클라이언트를 Accept하지 못함
            var socketContext = new SocketContext(tcpClient);
            _ = HandleClientReadAsync(socketContext, mCts.Token);
        }
    }

    #region 패킷 핸들러 함수 모음
    private async Task HandleLoginAsync(
        SocketContext socketContext, 
        CancellationToken cancellationToken)
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
        
        var payload = MemoryPackSerializer
            .Deserialize<EnterWorldReq>(socketContext.PayloadBuffer);

        try
        {
            if (mSessionManager.AddUserToWorld(payload.worldName, socketContext.Session.UserId))
            {
                socketContext.Session.CurrentWorld = payload.worldName;
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
        catch (KeyNotFoundException ex)
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

        var kind = MemoryPackSerializer
            .Deserialize<MessageKind>(socketContext.PayloadBuffer);

        switch (kind.Type)
        {
            case MessageType.Direct:
            {
                var payload = MemoryPackSerializer
                    .Deserialize<SendDirectMessageReq>(socketContext.PayloadBuffer);

                // TODO 저장 기능 필요
                if (!mSessionManager.LoginSessions.TryGetValue(payload.ReceiverUserId, out var session))
                    return;
                
                // TODO 개인에게 Notification 말이 안됨, 에러 처리
                
                await WritePacket(
                    session.Stream, 
                    new Packet<SendDirectMessageReq>(EPacket.SendMessage, payload),
                    cancellationToken);
                break;
            }
            case MessageType.World:
            case MessageType.Party:
            {
                if (socketContext.Session.CurrentParty is null)
                    return;

                switch (kind.Category)
                {
                    case MessageCategory.Notification:
                    {
                        var payload = MemoryPackSerializer
                            .Deserialize<Notification>(socketContext.PayloadBuffer);
                    
                        await BroadcastPacketToGroupAsync(
                            kind.Type,
                            socketContext.Session.CurrentParty,
                            new Packet<Notification>(EPacket.SendMessage, payload),
                            cancellationToken);
                        break;
                    }
                    case MessageCategory.Chat:
                    {
                        var payload = MemoryPackSerializer
                            .Deserialize<SendGroupMessageReq>(socketContext.PayloadBuffer);
                
                        await BroadcastPacketToGroupAsync(
                            kind.Type,
                            socketContext.Session.CurrentParty,
                            new Packet<SendGroupMessageReq>(EPacket.SendMessage, payload),
                            cancellationToken);
                        break;
                    }
                }
                
                // TODO 에러
                break;
            }
            case MessageType.Unknown:
            default:
                await SendResponsePacket<SendMessageRes>(
                    socketContext.Stream,
                    EPacket.SendMessage,
                    EResponseResult.SendMessageInvalidTarget,
                    cancellationToken);
                return;
        }

        await SendResponsePacket<SendMessageRes>(
            socketContext.Stream,
            EPacket.SendMessage,
            EResponseResult.Success,
            cancellationToken);
    }

    private async Task HandleDisconnectAsync(
        SocketContext socketContext, 
        CancellationToken cancellationToken)
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

        await SendResponsePacket<DisconnectRes>(
            socketContext.Stream,
            EPacket.Disconnect,
            EResponseResult.Success,
            cancellationToken);
        
        socketContext.Session.Disconnect();
    }
    #endregion

    private static async Task SendResponsePacket<T>(
        NetworkStream stream, 
        EPacket type, 
        EResponseResult code,
        CancellationToken cancellationToken) where T : struct, IPacketBody, IResponsePacket
    {
        var response = new T { Code = (int)code };
        await stream.WriteAsync(new Packet<T>(type, response).PacketBytes, cancellationToken);
    }

    private async Task BroadcastPacketToGroupAsync<T>(
        MessageType target,
        string name,
        Packet<T> packet,
        CancellationToken cancellationToken) where T : struct, IPacketBody
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
                sendTasks.Add(WritePacket(session.Stream, packet, cancellationToken));
        }

        await Task.WhenAll(sendTasks);
    }
}