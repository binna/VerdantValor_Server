using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SharedLibrary.Protocol.Common.ChatSocket;

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
    #endregion

    // TODO 나중에 동기화 고려 필요
    // 타임스템프 + userId 조합으로 string으로 변경 예정
    private static int roomCount = 1;
    
    public SocketServer(IPAddress ipAddress, int port)
    {
        mIpAddress = ipAddress;
        mPort = port;
    }
    
    // TODO 비정상 종료에 대한 로직 필요
    
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
        var lengthBuffer = new byte[AppEnum.LENGTH_FIELD_SIZE];
        byte[] payloadBuffer = [];
        
        var lengthRead = 0;
        var payloadLength = 0;
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
                if (lengthRead < AppEnum.LENGTH_FIELD_SIZE)
                {
                    var needHead = AppEnum.LENGTH_FIELD_SIZE - lengthRead;
                    var takeHead = Math.Min(needHead, read);

                    Buffer.BlockCopy(
                        readBuffer, offset,
                        lengthBuffer, lengthRead,
                        takeHead);

                    lengthRead += takeHead;
                    offset += takeHead;
                    remaining -= takeHead;

                    if (lengthRead < AppEnum.LENGTH_FIELD_SIZE)
                        break;

                    var beforeLength = payloadLength;
                    payloadLength = BinaryPrimitives.ReadInt16BigEndian(lengthBuffer);

                    if (beforeLength < payloadLength)
                        payloadBuffer = new byte[payloadLength];
                }

                var needData = payloadLength - payloadRead;
                var takeData = Math.Min(needData, read);

                Buffer.BlockCopy(
                    readBuffer, offset,
                    payloadBuffer, payloadRead,
                    takeData);

                payloadRead += takeData;
                offset += takeData;
                remaining -= takeData;

                if (payloadRead < payloadLength)
                    break;

                var type = BinaryPrimitives.ReadInt32BigEndian(payloadBuffer.AsSpan(0, 4));

                switch (type)
                {
                    case (int)AppEnum.PacketType.Login:
                    {
                        Console.WriteLine("in? - 1");
                        var sessionId = Encoding.UTF8.GetString(payloadBuffer, 4, 36);
                        var userId = BinaryPrimitives.ReadUInt64BigEndian(payloadBuffer.AsSpan(4 + 36, 8));
                        session = new Session(sessionId, userId, tcpClient);
                        Console.WriteLine($"sessionId:{sessionId}//userId:{userId}");

                        mConnectedSessions.TryAdd(userId, session);
                        Console.WriteLine("연결된 유저: " + mConnectedSessions.Count);
                        break;
                    }
                    case (int)AppEnum.PacketType.CreateRoom:
                    {
                        Console.WriteLine("in? - 2");
                        if (session == null)
                            break;

                        if (session.RoomId == 0)
                        {
                            var sessions = new ConcurrentDictionary<ulong, Session>();
                            sessions.TryAdd(session.UserId, session);
                            mRoomSessions.TryAdd(roomCount, sessions);
                            session.RoomId = roomCount;
                            roomCount++;
                            Console.WriteLine($"방 만들기 성공 {session.RoomId}");
                        }

                        break;
                    }
                    case (int)AppEnum.PacketType.EnterRoom:
                    {
                        if (session == null)
                            break;

                        if (session.RoomId == 0)
                        {
                            var roomId = BinaryPrimitives.ReadInt32BigEndian(payloadBuffer.AsSpan(4, 4));

                            if (mRoomSessions.TryGetValue(roomId, out var roomSessions))
                            {
                                roomSessions.TryAdd(session.UserId, session);
                                session.RoomId = roomId;
                                Console.WriteLine($"방 들어가기 성공 {session.RoomId}");
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
                            var message = Encoding.UTF8.GetString(payloadBuffer, 4, payloadLength - 4);
                            Console.WriteLine($"받음 : {message}");
                            _ = BroadcastToRoomAsync(session.RoomId, message, cancellationToken);
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

                lengthRead = 0;
                payloadLength = 0;
                payloadRead = 0;
            }
        }
    }

    private async Task BroadcastToRoomAsync(int roomId, string message, CancellationToken cancellationToken)
    {
        if (mRoomSessions.TryGetValue(roomId, out var roomSessions))
        {
            foreach (var client in roomSessions)
            {
                Console.WriteLine($"발송:{client.Value.SessionId}//{client.Value.UserId}");
                await client.Value.Stream.WriteAsync(Encoding.UTF8.GetBytes(message), cancellationToken);
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