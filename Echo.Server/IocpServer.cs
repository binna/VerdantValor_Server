using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Echo.Server;

public class SessionInfo
{
    public const int HEADER_LENGTH = sizeof(short);
    
    public string SessionId { get; }
    
    public readonly byte[] Header = new byte[HEADER_LENGTH];
    public int HeaderRead { get; set; }

    public byte[] Data { get; set; } = [];
    public int DataLength { get; set; }
    public int DataRead { get; set; }

    public SessionInfo(string sessionId)
    {
        SessionId = sessionId;
    }
}

public class IocpServer
{
    static ConcurrentDictionary<string, Socket> connectedClients = [];
    
    static Socket serverSocket = 
        new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    static IPEndPoint endPoint = new(IPAddress.Any, 20000);
    
    static ConcurrentBag<SocketAsyncEventArgs> receivePool = [];
    static ConcurrentBag<SocketAsyncEventArgs> acceptPool = [];
    
    static long msgsPerSec;

    static readonly Timer mpsTimer = new Timer(_ =>
    {
        var count = Interlocked.Exchange(ref msgsPerSec, 0);
        Console.WriteLine($"MPS: {count}");
    }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

    public static void Start(int backlogSize, int acceptPendingCount)
    {
        // 서버 소켓에 ip, port 할당
        serverSocket.Bind(endPoint);
        //Console.WriteLine("서버: Listen 시작");
        
        // 클라이언트들의 연결 요청을 대기하는 상태로 만듦
        // 백로그큐 = 클라이언트들의 연결 요청 대기실
        serverSocket.Listen(backlogSize);
        //Console.WriteLine("서버: Accept 대기");
        
        // AcceptAsync를 여러 개 동시에 등록해
        // accept를 동시에 대기(pending)시키는 구조다
        // 스레드를 만드는 것은 아니지만,
        // 이벤트 기반 처리에서 accept 처리량을 사전에 확보하는 효과가 있다.
        for (int i = 0; i < acceptPendingCount; i++)
        {
            var eventArgs = RentAcceptEventArgs();
            var pending = serverSocket.AcceptAsync(eventArgs);
            if (!pending)
                AcceptCompleted(serverSocket, eventArgs);
        }
        
        while (true)
            Thread.Sleep(1000);
    }
    
    private static void AcceptCompleted(object? sender, SocketAsyncEventArgs e)
    {
        var serverSocket = (Socket)sender!;
        var clientSocket = e.AcceptSocket!;
        
        if (e.SocketError == SocketError.Success)
        {
            string newSessionId;
        
            do
            {
                newSessionId = $"{Guid.NewGuid()}";
            } while (!connectedClients.TryAdd(newSessionId, clientSocket));
        
            //Console.WriteLine("서버: 클라 접속됨");
            //Console.WriteLine($"서버 연결됨: {clientSocket.RemoteEndPoint}/{newSessionId}");
        
            var eventArgs = RentReceiveEventArgs();
            eventArgs.UserToken = new SessionInfo(newSessionId);
   
            var bReceivePending = clientSocket.ReceiveAsync(eventArgs);
            if (!bReceivePending)
                ReceiveCompleted(clientSocket, eventArgs);
        }
        
        e.AcceptSocket = null;
        var bAcceptPending = serverSocket.AcceptAsync(e);
        if (!bAcceptPending)
            AcceptCompleted(serverSocket, e);
    }

    private static void ReceiveCompleted(object? sender, SocketAsyncEventArgs e)
    {
        var clientSocket = (Socket)sender!;
        
        if (e.UserToken is not SessionInfo sessionInfo)
        {
            //Console.WriteLine("서버: 클라 연결 종료");
            clientSocket.Dispose();
            ReturnReceiveEventArgs(e);
            return;
        }
        
        if (e.SocketError != SocketError.Success || e.BytesTransferred < 1)
        {
            //Console.WriteLine("서버: 클라 연결 종료");
            connectedClients.TryRemove(sessionInfo.SessionId, out _);
            clientSocket.Dispose();
            ReturnReceiveEventArgs(e);
            return;
        }

        if (e.Buffer != null)
        {
            var offset = e.Offset;
            var remaining = e.BytesTransferred;

            while (remaining > 0)
            {
                if (sessionInfo.HeaderRead < SessionInfo.HEADER_LENGTH)
                {
                    var needHead = SessionInfo.HEADER_LENGTH - sessionInfo.HeaderRead;
                    var takeHead = Math.Min(needHead, remaining);

                    Buffer.BlockCopy(
                        e.Buffer, offset,
                        sessionInfo.Header, sessionInfo.HeaderRead,
                        takeHead);

                    sessionInfo.HeaderRead += takeHead;
                    offset += takeHead;
                    remaining -= takeHead;

                    if (sessionInfo.HeaderRead < SessionInfo.HEADER_LENGTH)
                        break;

                    var beforeLength = sessionInfo.DataLength;
                    sessionInfo.DataLength = BinaryPrimitives.ReadUInt16BigEndian(sessionInfo.Header);
                    
                    if (beforeLength < sessionInfo.DataLength)
                        sessionInfo.Data = new byte[sessionInfo.DataLength];
                }
                
                var needData = sessionInfo.DataLength - sessionInfo.DataRead;
                var takeData = Math.Min(needData, remaining);

                Buffer.BlockCopy(
                    e.Buffer, offset, 
                    sessionInfo.Data, sessionInfo.DataRead, 
                    takeData);
                
                sessionInfo.DataRead += takeData;
                offset += takeData;
                remaining -= takeData;

                if (sessionInfo.DataRead < sessionInfo.DataLength)
                    break;

                //Console.WriteLine(
                //    Encoding.UTF8.GetString(sessionInfo.Data, 0, sessionInfo.DataLength));
                Interlocked.Increment(ref msgsPerSec);
                
                var messageBuffer = new byte[SessionInfo.HEADER_LENGTH + sessionInfo.DataLength];
    
                Array.Copy(
                    sessionInfo.Header, 0, 
                    messageBuffer, 0, 
                    SessionInfo.HEADER_LENGTH);
                Array.Copy(
                    sessionInfo.Data, 0, 
                    messageBuffer, SessionInfo.HEADER_LENGTH, 
                    sessionInfo.DataLength);
                
                foreach (var client in connectedClients)
                {
                    client.Value.Send(messageBuffer);
                }
                
                sessionInfo.HeaderRead = 0;
                sessionInfo.DataLength = 0;
                sessionInfo.DataRead = 0;
            }
        }
        
        var bReceivePending = clientSocket.ReceiveAsync(e);
        if (!bReceivePending)
            ReceiveCompleted(clientSocket, e);
    }
    
    private static SocketAsyncEventArgs RentReceiveEventArgs()
    {
        if (!receivePool.TryTake(out var e))
        {
            e = new SocketAsyncEventArgs();
            e.Completed += ReceiveCompleted;
            
            var buffer = new byte[256];
            e.SetBuffer(buffer, 0, buffer.Length);
        }

        return e;
    }
    
    private static void ReturnReceiveEventArgs(SocketAsyncEventArgs e)
    {
        e.UserToken = null;
        e.AcceptSocket = null;
        receivePool.Add(e);
    }
    
    private static SocketAsyncEventArgs RentAcceptEventArgs()
    {
        if (!acceptPool.TryTake(out var e))
        {
            e = new SocketAsyncEventArgs();
            e.Completed += AcceptCompleted;
        }

        return e;
    }
}