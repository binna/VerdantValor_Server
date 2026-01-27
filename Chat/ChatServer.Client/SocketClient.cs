using System.Net;
using System.Net.Sockets;
using VerdantValorShared.Common.ChatServer;
using VerdantValorShared.Packet;
using VerdantValorShared.Packet.ChatServer;

namespace ChatServer.Client;

public class SocketClient
{
    private readonly IPAddress mIpAddress;
    private readonly int mPort;
    
    private TcpClient? mTcpClient;
    private CancellationTokenSource? mCts;
    
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
        mTcpClient = new TcpClient(mIpAddress.ToString(), mPort);
        
        await HandleReadAsync(mTcpClient, mCts.Token);
    }

    public async Task SendLoginAsync()
    {
        if (mTcpClient == null)
            throw new InvalidOperationException("Client is not running.");
        
        var stream = mTcpClient.GetStream();
        
        Console.WriteLine("Enter UserId : ");
        var userIdInput = Console.ReadLine();
        if (ulong.TryParse(userIdInput, out var userId))
        {
            var packet = 
                new Packet<Login>(new Login
                {
                    SessionId = $"{Guid.NewGuid()}",
                    UserId = userId
                });
            await stream.WriteAsync(packet.From(), mCts!.Token);
        }
    }

    public async Task SendRoomListAsync()
    {
        if (mTcpClient == null)
            throw new InvalidOperationException("Client is not running.");
        
        var stream = mTcpClient.GetStream();
        
        var packet = new Packet<RoomList>(new RoomList());
        await stream.WriteAsync(packet.From(), mCts!.Token);
    }

    public async Task SendCreateRoomAsync()
    {
        if (mTcpClient == null)
            throw new InvalidOperationException("Client is not running.");
        
        var stream = mTcpClient.GetStream();
        
        var packet = new Packet<CreateRoom>(new CreateRoom());
        await stream.WriteAsync(packet.From(), mCts!.Token);
        await HandleWriteAsync(mTcpClient, mCts!.Token);
        
    }

    public async Task SendEnterRoomAsync()
    {
        if (mTcpClient == null)
            throw new InvalidOperationException("Client is not running.");
        
        var stream = mTcpClient.GetStream();
        
        Console.WriteLine("Enter RoomId : ");
        var roomIdInput = Console.ReadLine();
        if (int.TryParse(roomIdInput, out var roomId))
        {
            var packet = new Packet<EnterRoom>(
                new EnterRoom
                {
                    RoomId = roomId
                });
            await stream.WriteAsync(packet.From(), mCts!.Token);
            await HandleWriteAsync(mTcpClient, mCts!.Token);
        }
    }
    
    private static async Task HandleWriteAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        var stream = tcpClient.GetStream();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var messageInput = Console.ReadLine();

            if (messageInput == null)
                break;

            if (messageInput.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
            {
                var exitRoomPacket = new Packet<ExitRoom>(new ExitRoom()); 
                await stream.WriteAsync(exitRoomPacket.From(), cancellationToken);
                return;
            }
  
            var sendMessagePacket = 
                new Packet<SendMessage>(new SendMessage { Message = messageInput });
            await stream.WriteAsync(sendMessagePacket.From(), cancellationToken);
        }
    }
    
    private async Task HandleReadAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
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
                Stop();
                break;
            }

            var offset = 0;
            var remaining = read;
            
            while (remaining > 0)
            {
                var needHeader = Header.HEADER_SIZE - headerRead;
                var takeHeader = Math.Min(needHeader, remaining);
                
                Buffer.BlockCopy(
                    readBuffer, offset, 
                    headerBuffer, headerRead, 
                    Header.HEADER_SIZE);

                headerRead += takeHeader;
                offset += takeHeader;
                remaining -= takeHeader;

                if (headerRead < Header.HEADER_SIZE)
                    break;

                var beforePayloadLength = header.PayloadLength;
                
                header.Parse(headerBuffer);
                
                if (beforePayloadLength < header.PayloadLength)
                    payloadBuffer = new byte[header.PayloadLength];
                
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

                switch (header.Type)
                {
                    case (int)AppEnum.PacketType.DeleteRoom:
                    {
                        // TODO 방에서 쫓겨나는게 필요
                        break;
                    }
                    case (int)AppEnum.PacketType.RoomList:
                    {
                        var payload = new RoomList();
                        payload.Parse(payloadBuffer);

                        Console.WriteLine("Room List=================");
                        foreach (var roomId in payload.RoomIds)
                        {
                            Console.WriteLine(roomId);
                        }
                        Console.WriteLine("==========================");
                        break;
                    }
                    case (int)AppEnum.PacketType.SendMessage:
                    {
                        var payload = new SendMessage();
                        payload.Parse(payloadBuffer);
                        Console.WriteLine($"Message: {payload.Message}");
                        break;
                    }
                    case (int)AppEnum.PacketType.RoomNotification:
                    {
                        var payload = new RoomNotification();
                        payload.Parse(payloadBuffer);
                        Console.WriteLine($"Message: {payload.Notification}");
                        break;
                    }
                }
            }
            headerRead = 0;
            payloadRead = 0;
        }
    }
    
    public void Stop()
    {
        if (mTcpClient == null)
            return;
        
        mCts!.Cancel();
        
        mTcpClient.Dispose();
        mTcpClient = null;
    }
}