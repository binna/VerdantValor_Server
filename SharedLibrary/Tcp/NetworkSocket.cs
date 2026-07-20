using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using MemoryPack;
using Protocol.Chat.Frames;
using Shared.Constants;
using Shared.Types;

namespace Tcp;

public abstract class NetworkSocket : IDisposable
{
    private enum ReadPacketReturn
    {
        NeedMoreData,
        PacketReady,
        BufferDrained
    }

    private Dictionary<EPacket, Func<SocketContext, CancellationToken, Task>> mPacketHandlers = [];
    
    private readonly CancellationTokenSource mCts;
    
    protected readonly CancellationToken mToken;
    protected readonly Config mConfig;
    
    protected Dictionary<EPacket, Func<SocketContext, CancellationToken, Task>> PacketHandlers
    {
        set
        {
            if (mPacketHandlers.Count > 0)
                throw new InvalidOperationException("PacketHandlers는 한 번만 설정할 수 있습니다.");

            mPacketHandlers = value;
        }
    }
    
    protected NetworkSocket(
        CancellationTokenSource cts = default)
    {
        mCts = cts ?? throw new ArgumentNullException(nameof(cts), "필수 값이 누락되었습니다.");
        mToken = cts.Token;
        
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var jsonText = File.ReadAllText(path);
        mConfig = JsonSerializer.Deserialize<Config>(jsonText) ?? throw new Exception("Invalid Configuration File");
    }

    public abstract Task StartAsync(IPAddress ipAddress, int port);
    public abstract Task AcceptAsync();
    protected abstract Task DisconnectClientAsync(SocketContext socketContext);
    
    protected abstract Task CheckSessionsAsync();
    
    public static bool IsSocketAlive(TcpClient client)
    {
        try
        {
            var socket = client.Client;

            // 읽기 가능한 상태 확인
            // 단, 상대방이 연결을 끊었을 때도 true가 된다
            var isReadable = socket.Poll(0, SelectMode.SelectRead);

            // 대기 중인 데이터가 있는지 확인
            var noDataAvailable = socket.Available == 0;

            // 읽기는 가능한데 데이터가 없다 = 연결이 끊긴 것
            return !(isReadable && noDataAvailable);
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    protected async Task StartConnectionCheckAsync(int intervalMinutes)
    {
        while (!mToken.IsCancellationRequested)
        {
            try
            {
                await CheckSessionsAsync();
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), mToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Session Check Failed: {ex.Message}");
            }
        }
    }

    protected async Task HandleClientReadAsync(
        SocketContext socketContext, 
        CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var read = await socketContext.Stream.ReadAsync(
                    socketContext.ReadBuffer, token);

                if (read == 0)
                {
                    Console.WriteLine($"[Info] Client Disconnected");
                    return;
                }

                socketContext.Offset = 0;
                socketContext.Remaining = read;

                while (!token.IsCancellationRequested)
                {
                    var result = ReadPacket(socketContext);

                    if (result == ReadPacketReturn.NeedMoreData)
                        break;

                    if (Enum.IsDefined(socketContext.Header.PacketType))
                        await mPacketHandlers[socketContext.Header.PacketType](socketContext, token);

                    if (result == ReadPacketReturn.PacketReady)
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 취소 시그널은 의도된 종료이므로 에러가 아님
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] HandleClientReadAsync Error - {ex.Message}");
        }
        finally
        {
            // 정상적으로 통신 중일 때는 이 메서드가 끝나지 않는다.
            // 즉 finally에 도달했다는 것 자체가 연결이 끊겼음을 의미한다.
            await DisconnectClientAsync(socketContext);
        }   
    }
    
    protected static async Task WritePacket<T>(
        NetworkStream stream, 
        Packet<T> message, 
        CancellationToken cancellationToken) where T : struct, IPacketBody
    {
        await stream.WriteAsync(message.PacketBytes, cancellationToken);
    }
    
    private static ReadPacketReturn ReadPacket(SocketContext socketContext)
    {
        while (socketContext.Remaining > 0)
        {
            if (socketContext.HeaderRead < AppConstant.HEADER_SIZE)
            {
                var needHeader = AppConstant.HEADER_SIZE - socketContext.HeaderRead;
                var takeHeader = Math.Min(needHeader, socketContext.Remaining);

                Buffer.BlockCopy(
                    socketContext.ReadBuffer, socketContext.Offset,
                    socketContext.HeaderBuffer, socketContext.HeaderRead,
                    takeHeader);

                socketContext.HeaderRead += takeHeader;
                socketContext.Offset += takeHeader;
                socketContext.Remaining -= takeHeader;

                if (socketContext.HeaderRead < AppConstant.HEADER_SIZE)
                    return ReadPacketReturn.NeedMoreData;

                var beforePayloadLength = socketContext.Header.PayloadSize;

                socketContext.Header = 
                    MemoryPackSerializer.Deserialize<PacketHeader>(socketContext.HeaderBuffer);

                if (beforePayloadLength < socketContext.Header.PayloadSize)
                    socketContext.PayloadBuffer = new byte[socketContext.Header.PayloadSize];
            }

            var needPayLoad = socketContext.Header.PayloadSize - socketContext.PayloadRead;
            var takePayLoad = Math.Min(needPayLoad, socketContext.Remaining);

            Buffer.BlockCopy(
                socketContext.ReadBuffer, socketContext.Offset,
                socketContext.PayloadBuffer, socketContext.PayloadRead,
                takePayLoad);

            socketContext.PayloadRead += takePayLoad;
            socketContext.Offset += takePayLoad;
            socketContext.Remaining -= takePayLoad;

            if (socketContext.PayloadRead < socketContext.Header.PayloadSize)
                return ReadPacketReturn.NeedMoreData;
            
            socketContext.HeaderRead = 0;
            socketContext.PayloadRead = 0;
            
            if (socketContext.Remaining  > 0)
                return ReadPacketReturn.BufferDrained;
        }

        return ReadPacketReturn.PacketReady;
    }

    public void Dispose()
    {
        mCts.Cancel();
        mCts.Dispose();
    }
}