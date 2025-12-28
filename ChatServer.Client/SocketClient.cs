using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SharedLibrary.Protocol.Common.ChatServer;
using SharedLibrary.Protocol.Packet;
using SharedLibrary.Protocol.Packet.ChatServer;

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
        {
            var payload = new LoginPayload
            {
                SessionId = $"{Guid.NewGuid()}",
                UserId = 1
            };
            var header = new Header
            {
                Type = (int)AppEnum.PacketType.Login,
                PayloadLength = payload.PayloadSize 
            };
        
            var packet = new Packet<LoginPayload>(header, payload);
            await stream.WriteAsync(packet.From(), cancellationToken);
        }
        
        // 방 만들기
        {
            var payload = new CreateRoomPayload();
            var header = new Header
            {
                Type = (int)AppEnum.PacketType.CreateRoom,
                PayloadLength = payload.PayloadSize 
            };
        
            var packet = new Packet<CreateRoomPayload>(header, payload);
            await stream.WriteAsync(packet.From(), cancellationToken);
        }
        
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

            if (str == null || str.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
                return;

            var payload = new SendMessagePayload
            {
                Message = str
            };
            var header = new Header
            {
                Type = (int)AppEnum.PacketType.SendMessage,
                PayloadLength = payload.PayloadSize 
            };
        
            var packet = new Packet<SendMessagePayload>(header, payload);
            
            stream.WriteAsync(packet.From(), cancellationToken);
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