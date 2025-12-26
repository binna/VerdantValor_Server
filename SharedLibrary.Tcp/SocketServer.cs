using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SharedLibrary.Tcp;

public sealed class Session
{
    private readonly TcpClient mClient;
    
    public string SessionId { get; }
    public ulong UserId { get; }
    public int RoomId { get; set; }

    public Session(string sessionId, ulong userId, TcpClient client)
    {
        SessionId = sessionId;
        UserId = userId;
        mClient = client;
    }

    public NetworkStream Stream => mClient.GetStream();
    public void Disconnect() => mClient.Dispose();
}

public enum Type
{
    Login = 0,
    CreateRoom = 1,
    EnterRoom = 2,
    ExitRoom = 3,
    SendMessage = 4,
    Disconnect = 5,
}

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

        while (!cancellationToken.IsCancellationRequested)
        {
            var buffer = new byte[1024];
            var read = await stream.ReadAsync(buffer, cancellationToken);
        
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
            
            var type = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(0, 4));

            switch (type)
            {
                case (int)Type.Login:
                {
                    var sessionId = Encoding.UTF8.GetString(buffer, 4, 36);
                    var userId = BinaryPrimitives.ReadUInt64BigEndian(buffer.AsSpan(4 + 36, 8));
                    session = new Session(sessionId, userId, tcpClient);
                    Console.WriteLine($"sessionId:{sessionId}//userId:{userId}");
                    
                    mConnectedSessions.TryAdd(userId, session);
                    Console.WriteLine("연결된 유저: " + mConnectedSessions.Count);
                    break;
                }
                case (int)Type.CreateRoom:
                {
                    Console.WriteLine("in?");
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
                case (int)Type.EnterRoom:
                {
                    if (session == null)
                        break;

                    if (session.RoomId == 0)
                    {
                        var roomId = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(4, 4));

                        if (mRoomSessions.TryGetValue(roomId, out var roomSessions))
                        {
                            roomSessions.TryAdd(session.UserId, session);
                            session.RoomId = roomId;
                            Console.WriteLine($"방 들어가기 성공 {session.RoomId}");
                        }
                    }

                    break;
                }
                case (int)Type.ExitRoom:
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
                case (int)Type.SendMessage:
                {
                    if (session == null)
                        break;

                    if (session.RoomId != 0)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, read);
                        Console.WriteLine($"받음 : {message}");
                        _ = BroadcastToRoomAsync(session.RoomId, message, cancellationToken);
                    }
                    break;
                }
                case (int)Type.Disconnect:
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