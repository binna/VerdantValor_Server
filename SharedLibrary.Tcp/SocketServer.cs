using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using SharedLibrary.Protocol.Common.ChatServer;
using SharedLibrary.Protocol.Packet;
using SharedLibrary.Protocol.Packet.ChatServer;

namespace SharedLibrary.Tcp;

public class SocketServer
{
    private readonly IPAddress mIpAddress;
    private readonly int mPort;
    
    private TcpListener? mListener;
    private CancellationTokenSource mCts = new();
    
    #region TODO 별도의 ClientSessionManager 만들어야 함
    private static ConcurrentDictionary<ulong, Session> mConnectedSessions = [];
    private static ConcurrentDictionary<int, ConcurrentDictionary<ulong, Session>> mRoomSessions = [];
    private static byte[] mRoomIdsPacket = [];
    private static bool mbUpdated = false;
    
    // TODO 나중에 동기화 고려 필요
    //      타임스템프 + userId 조합으로 string으로 변경 예정
    private static int roomCount = 0;
    #endregion

    private static readonly Timer UpdateRoomIdsTimer = 
        new(UpdateRoomIds, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    
    public SocketServer(IPAddress ipAddress, int port)
    {
        mIpAddress = ipAddress;
        mPort = port;
    }
    
    // TODO 비정상 종료에 대한 로직 필요
    //      Timer 함수로 1분에 한번씩 확인하는 식으로 작업 예정
    
    // TODO 여러개의 유저 테스트 필요
    
    private static void UpdateRoomIds(object? o)
    {
        if (!mbUpdated)
            return;
        
        Console.WriteLine("변경 방리스트 정보");
        mbUpdated = false;
        
        var roomIds = mRoomSessions.Keys.ToArray();

        if (roomIds.Length == 0)
            return;
        
        var payload = new RoomList
        {
            RoomCount = roomIds.Length,
            RoomIds = roomIds
        };
        var header = new Header
        {
            Type = (int)AppEnum.PacketType.RoomList,
            PayloadLength = payload.PayloadSize 
        };
        
        var packet = new Packet<RoomList>(header, payload);
        
        mRoomIdsPacket = packet.From();
    }
    
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (mListener != null)
            throw new InvalidOperationException("Server is already running.");
        
        mCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var cancelToken = mCts.Token;
        
        mListener = new TcpListener(mIpAddress, mPort);
        mListener.Start();

        while (!cancelToken.IsCancellationRequested)
        {
            var tcpClient = await mListener.AcceptTcpClientAsync(cancelToken);
            Console.WriteLine($"[SERVER] Client connected: {tcpClient.Client.RemoteEndPoint}");
            _ = HandleClientAsync(tcpClient, cancelToken);
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        Session? session = null;
        
        var stream = tcpClient.GetStream();
        
        var readBuffer = new byte[1024];
        
        var header = new Header();
        var headerBuffer = new byte[Header.HEADER_SIZE];
        byte[] payloadBuffer = [];
        
        var headerRead = 0;
        var payloadRead = 0;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var read = await stream.ReadAsync(readBuffer, cancellationToken);
        
            if (read == 0)
            {
                Console.WriteLine("[SERVER] Client disconnected");
                if (session != null)
                {
                    if (mRoomSessions.TryGetValue(session.RoomId, out var roomSessions))
                        roomSessions.TryRemove(session.UserId, out _);
                }
                return;
            }
            
            var offset = 0;
            var remaining = read;

            while (remaining > 0)
            {
                if (headerRead < Header.HEADER_SIZE)
                {
                    var needHeader = Header.HEADER_SIZE - headerRead;
                    var takeHeader = Math.Min(needHeader, remaining);

                    Buffer.BlockCopy(
                        readBuffer, offset,
                        headerBuffer, headerRead,
                        takeHeader);

                    headerRead += takeHeader;
                    offset += takeHeader;
                    remaining -= takeHeader;

                    if (headerRead < Header.HEADER_SIZE)
                        break;
                    
                    var beforePayloadLength = header.PayloadLength;
                    
                    header.Parse(headerBuffer);
                    
                    if (beforePayloadLength < header.PayloadLength)
                        payloadBuffer = new byte[header.PayloadLength];
                }

                var needPayLoad = header.PayloadLength - payloadRead;
                var takePayLoad = Math.Min(needPayLoad, remaining);

                Buffer.BlockCopy(
                    readBuffer, offset,
                    payloadBuffer, payloadRead,
                    takePayLoad);

                payloadRead += takePayLoad;
                offset += takePayLoad;
                remaining -= takePayLoad;

                if (payloadRead < header.PayloadLength)
                    break;

                Console.WriteLine($">> {header.Type}");

                switch (header.Type)
                {
                    case (int)AppEnum.PacketType.Login:
                    {
                        // TODO 유효성 검사가 필요
                        var payload = new Login();
                        payload.Parse(payloadBuffer);
                        
                        session = new Session(payload.SessionId, payload.UserId, tcpClient);
                        Console.WriteLine($"sessionId:{payload.SessionId}//userId:{payload.UserId}");

                        mConnectedSessions.TryAdd(payload.UserId, session);
                        Console.WriteLine("연결된 유저: " + mConnectedSessions.Count);
                        break;
                    }
                    case (int)AppEnum.PacketType.CreateRoom:
                    {
                        if (session == null)
                            break;

                        if (session.RoomId == 0)
                        {
                            var sessions = new ConcurrentDictionary<ulong, Session>();
                            sessions.TryAdd(session.UserId, session);

                            int newRoomId;
                            do
                            {
                                newRoomId = Interlocked.Increment(ref roomCount);
                            } while (!mRoomSessions.TryAdd(newRoomId, sessions));
                            session.RoomId = newRoomId;
                            mbUpdated = true;
                            Console.WriteLine($"방 만들기 성공 {session.RoomId}");
                        }
                        break;
                    }
                    case (int)AppEnum.PacketType.RoomList:
                    {
                        if (session == null)
                            break;
                        
                        await session.Stream.WriteAsync(mRoomIdsPacket, cancellationToken);
                        break;
                    }
                    case (int)AppEnum.PacketType.EnterRoom:
                    {
                        if (session == null)
                            break;

                        if (session.RoomId == 0)
                        {
                            var payload = new EnterRoom();
                            payload.Parse(payloadBuffer);

                            if (mRoomSessions.TryGetValue(payload.RoomId, out var roomSessions))
                            {
                                roomSessions.TryAdd(session.UserId, session);
                                session.RoomId = payload.RoomId;
                                
                                _ = BroadcastRoomNotificationAsync(
                                    session.RoomId, 
                                    $"{session.UserId}가 입장하셨습니다.", 
                                    cancellationToken);
                                Console.WriteLine($"방 들어가기 성공 {session.RoomId}");
                            }
                            else
                            {
                                Console.WriteLine($"검색된 방이 없습니다.");
                            }
                        }
                        break;
                    }
                    case (int)AppEnum.PacketType.ExitRoom:
                    {
                        if (session == null || session.RoomId == 0)
                            break;

                        if (mRoomSessions.TryGetValue(session.RoomId, out var roomSessions))
                        {
                            roomSessions.TryRemove(session.UserId, out _);
                            session.RoomId = 0;
                            mbUpdated = true;
                            _ = BroadcastRoomNotificationAsync(
                                session.RoomId, 
                                $"{session.UserId}가 퇴장하셨습니다.", 
                                cancellationToken);
                            Console.WriteLine($"방 삭제 성공 {session.RoomId}");
                        }
                        break;
                    }
                    case (int)AppEnum.PacketType.SendMessage:
                    {
                        if (session == null)
                            break;

                        if (session.RoomId != 0)
                        {
                            var payload = new SendMessage();
                            payload.Parse(payloadBuffer);
                            Console.WriteLine($"받음 : {payload.Message}");
                            
                            var packet = new Packet<SendMessage>(header, payload);
                            _ = BroadcastChatMessageAsync(session.RoomId, packet, cancellationToken);
                        }
                        break;
                    }
                    case (int)AppEnum.PacketType.Disconnect:
                    {
                        Console.WriteLine("[SERVER] Client disconnected");
                        if (session != null)
                        {
                            if (mRoomSessions.TryGetValue(session.RoomId, out var roomSessions))
                                roomSessions.TryRemove(session.UserId, out _);
                        }
                        return;
                    }
                }
                headerRead = 0;
                payloadRead = 0;
            }
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
        var payload = new RoomNotification
        {
            Notification = notificationMessage
        };
        var header = new Header
        {
            Type = (int)AppEnum.PacketType.RoomNotification,
            PayloadLength = payload.PayloadSize 
        };
        
        var packet = new Packet<RoomNotification>(header, payload);
        
        if (mRoomSessions.TryGetValue(roomId, out var roomSessions))
        {
            var packetBuffer = packet.From();
            
            foreach (var client in roomSessions)
            {
                Console.WriteLine($"공지발송:{client.Value.SessionId}//{client.Value.UserId}");
                await client.Value.Stream.WriteAsync(packetBuffer, cancellationToken);
            }
        }
    }

    public void StopAsync()
    {
        if (mListener == null)
            return;
        
        mCts.Cancel();
        
        mListener.Stop();
        mListener = null;

        foreach (var session in mConnectedSessions)
        {
            if (mConnectedSessions.TryRemove(session.Key, out _))
                session.Value.Disconnect();
        }

        mRoomSessions.Clear();
    }
}