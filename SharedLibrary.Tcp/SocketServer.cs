using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using SharedLibrary.Protocol.Common.ChatServer;
using SharedLibrary.Protocol.Packet;

namespace SharedLibrary.Tcp;

public abstract class SocketServer
{
    private readonly Dictionary<AppEnum.PacketType, Action<SocketContext, CancellationToken>> mPacketHandlers;
    
    private readonly IPAddress mIpAddress;
    private readonly int mPort;
    
    private TcpListener? mListener;
    private CancellationTokenSource? mCts;
    
    protected 
        static ConcurrentDictionary<ulong, Session> mConnectedSessions = [];
    
    public SocketServer(
        IPAddress ipAddress, int port, 
        Dictionary<AppEnum.PacketType, Action<SocketContext, CancellationToken>> packetHandlers)
    {
        mIpAddress = ipAddress;
        mPort = port;
        mPacketHandlers = packetHandlers;
    }
    
    // TODO 비정상 종료에 대한 로직 필요
    //      Timer 함수로 1분에 한번씩 확인하는 식으로 작업 예정
    
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (mListener != null)
            throw new InvalidOperationException("Server is already running.");
        
        mCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        mListener = new TcpListener(mIpAddress, mPort);
        mListener.Start();

        while (!mCts.Token.IsCancellationRequested)
        {
            var tcpClient = await mListener.AcceptTcpClientAsync(mCts.Token);
            Console.WriteLine($"[SERVER] Client connected: {tcpClient.Client.RemoteEndPoint}");
            _ = HandleClientAsync(tcpClient, mCts.Token);
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

                if (Enum.IsDefined(typeof(AppEnum.PacketType), header.Type))
                {
                    var socketContext = new SocketContext(tcpClient, session, header, payloadBuffer);
                    mPacketHandlers[(AppEnum.PacketType)header.Type](socketContext, cancellationToken);
                }
                
                headerRead = 0;
                payloadRead = 0;
            }
        }
    }

    public virtual void Stop()
    {
        if (mListener == null)
            return;
        
        mCts!.Cancel();
        
        mListener.Stop();
        mListener = null;

        foreach (var session in mConnectedSessions)
        {
            if (mConnectedSessions.TryRemove(session.Key, out _))
                session.Value.Disconnect();
        }
    }
}