using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SharedLibrary.Protocol.Common.ChatSocket;

namespace ChatServer.Client;

public class SocketClient
{
    private readonly IPAddress mIpAddress;
    private readonly int mPort;
    
    private TcpClient? mTcpClient;
    private CancellationTokenSource mCts = new();
    
    public SocketClient(IPAddress ipAddress, int port)
    {
        mIpAddress = ipAddress;
        mPort = port;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (mTcpClient != null)
            throw new InvalidOperationException("Client is already running.");
        
        mCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var cancelToken = mCts.Token;
        
        mTcpClient = new TcpClient(mIpAddress.ToString(), mPort);

        var stream = mTcpClient.GetStream();

        // 로그인
        var type = new byte[4];
        var sessionId = Encoding.UTF8.GetBytes($"{Guid.NewGuid()}");
        var userId = new byte[8];
        
        BinaryPrimitives.WriteInt32BigEndian(type, (int)AppEnum.PacketType.Login);
        BinaryPrimitives.WriteUInt64BigEndian(userId, 1UL);

        var length = new byte[2];
        
        BinaryPrimitives.WriteInt16BigEndian(length, 4 + 36 + 8);
        
        var messageBuffer = new byte[2 + 4 + 36 + 8];
        Array.Copy(
            length, 0, 
            messageBuffer, 0, 
            2);
        Array.Copy(
            type, 0, 
            messageBuffer, 2, 
            type.Length);
        Array.Copy(
            sessionId, 0, 
            messageBuffer, 2 + type.Length, 
            sessionId.Length);
        Array.Copy(
            userId, 0, 
            messageBuffer, 2 + type.Length + sessionId.Length, 
            userId.Length);
        
        await stream.WriteAsync(messageBuffer, cancellationToken);

        Console.WriteLine("1");
        
        // 방 만들기
        BinaryPrimitives.WriteInt32BigEndian(type, (int)AppEnum.PacketType.CreateRoom);
        BinaryPrimitives.WriteInt16BigEndian(length, 2 + 4);
        
        messageBuffer = new byte[2 + 4];
        Array.Copy(
            length, 0, 
            messageBuffer, 0, 
            2);
        Array.Copy(
            type, 0, 
            messageBuffer, 2, 
            type.Length);
        
        await stream.WriteAsync(messageBuffer, cancellationToken);
        
        Console.WriteLine("2");
        
        var readTask  = HandleClientSendAsync(mTcpClient, cancelToken);
        var writeTask = Task.Run(() => HandleClientWriteAsync(mTcpClient, cancelToken), cancelToken);
        
        await Task.WhenAny(readTask, writeTask);
    }
    
    private void HandleClientWriteAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        var stream = tcpClient.GetStream();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var str = Console.ReadLine();

            if (str == null)
                return;
            
            var header = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(header, (int)AppEnum.PacketType.SendMessage);
            var buffer = Encoding.UTF8.GetBytes(str);
            var messageBuffer = new byte[4 + buffer.Length];
            Array.Copy(
                header, 0, 
                messageBuffer, 0, 
                header.Length);
            Array.Copy(
                buffer, 0, 
                messageBuffer, 4, 
                buffer.Length);
            
            stream.WriteAsync(messageBuffer, cancellationToken);
        }
    }
    
    private async Task HandleClientSendAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        var stream = tcpClient.GetStream();
        var buffer = new byte[1024];

        while (!cancellationToken.IsCancellationRequested)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);

            if (read == 0)
            {
                Console.WriteLine("[SERVER] Client disconnected");
                StopAsync();
                break;
            }
            
            var msg = Encoding.UTF8.GetString(buffer, 0, read);
            Console.WriteLine($"받음 : {msg}");
        }
    }
    
    public void StopAsync()
    {
        Console.WriteLine("호출!");
        if (mTcpClient == null)
            return;
        
        mCts.Cancel();
        
        mTcpClient.Dispose();
        mTcpClient = null;
    }
}