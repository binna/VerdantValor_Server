using System.Net;
using System.Net.Sockets;
using MemoryPack;
using Protocol.Chat.Frames;
using Shared.Constants;
using Shared.Types;

namespace Tcp;

public abstract class NetworkSocket : IDisposable
{
    private enum EReadPacketReturn
    {
        NeedMoreData,
        PacketReady,
        BufferDrained
    }

    private bool mbDisposed;
    private Dictionary<EPacket, Func<SocketContext, CancellationToken, Task>> mPacketHandlers;
    
    private readonly CancellationTokenSource mCts;
    protected readonly CancellationToken mToken;

    protected NetworkSocket(CancellationTokenSource cts = default)
    {
        mCts = cts ?? throw new ArgumentNullException(nameof(cts), "A required value is missing.");
        mToken = cts.Token;
    }

    public abstract Task StartAsync(IPAddress ipAddress, int port);
    protected abstract Task AcceptAsync();
    protected abstract Task DisconnectClientAsync(SocketContext socketContext);
    protected abstract Task StartHeartbeatLoopAsync();
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
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (mbDisposed)
            return;

        if (disposing)
        {
            mCts.Cancel();
            mCts.Dispose();
        }

        mbDisposed = true;
    }
    
    protected virtual void LogInfo(string message) => Console.WriteLine($"[Info] {message}");
    protected virtual void LogError(string message) => Console.WriteLine($"[Error] {message}");
    
    protected void RegisterPacketHandlers(Dictionary<EPacket, Func<SocketContext, CancellationToken, Task>> packetHandlers) => mPacketHandlers = packetHandlers;
    
    protected async Task ConnectionCheckLoopAsync(int intervalMinutes)
    {
        while (!mToken.IsCancellationRequested)
        {
            try
            {
                await CheckSessionsAsync();
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), mToken);
            }
            catch (OperationCanceledException)
            {
                // 취소로 인한 종료는 정상 흐름이므로 루프를 빠져나간다.
                break;
            }
            catch (Exception ex)
            {
                LogError($"Session Check Failed: {ex.Message}");
            }
        }
    }

    protected async Task HandleClientReadAsync(SocketContext socketContext, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var read = await socketContext.Stream.ReadAsync(
                    socketContext.ReadBuffer, token);

                if (read == 0)
                {
                    LogInfo("Client Disconnected");
                    return;
                }

                socketContext.Offset = 0;
                socketContext.Remaining = read;

                while (!token.IsCancellationRequested)
                {
                    var result = ReadPacket(socketContext);

                    if (result == EReadPacketReturn.NeedMoreData)
                        break;

                    var packetType = socketContext.Header.PacketType;
                    
                    if (!Enum.IsDefined(packetType))
                    {
                        LogError($"Unknown Packet Type: {packetType}");
                        break;
                    }
                    
                    if (!mPacketHandlers.TryGetValue(packetType, out var handler))
                    {
                        LogError($"No Handler Registered For Packet Type: {packetType}");
                        break;
                    }

                    await handler(socketContext, token);

                    if (result == EReadPacketReturn.PacketReady)
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 취소로 인한 종료는 정상 흐름이므로 루프를 빠져나간다.
        }
        catch (Exception ex)
        {
            LogError($"HandleClientReadAsync Error - {ex.Message}");
        }
        finally
        {
            // 정상적으로 통신 중일 때는 이 메서드가 끝나지 않는다.
            // 즉 finally에 도달했다는 것 자체가 연결이 끊겼음을 의미한다.
            await DisconnectClientAsync(socketContext);
        }   
    }
    
    protected static async Task WritePacket<T>(NetworkStream stream, Packet<T> message, CancellationToken cancellationToken) where T : struct, IPacketBody
    {
        await stream.WriteAsync(message.PacketBytes, cancellationToken);
    }
    
    private static EReadPacketReturn ReadPacket(SocketContext socketContext)
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
                    return EReadPacketReturn.NeedMoreData;

                var beforePayloadLength = socketContext.Header.PayloadSize;

                socketContext.Header = 
                    MemoryPackSerializer.Deserialize<PacketHeader>(socketContext.HeaderBuffer);

                if (beforePayloadLength < socketContext.Header.PayloadSize)
                    socketContext.PayloadBuffer = new byte[socketContext.Header.PayloadSize];
            }

            var needPayLoad = socketContext.Header.PayloadSize - socketContext.PayloadRead;
            var takePayload = Math.Min(needPayLoad, socketContext.Remaining);

            Buffer.BlockCopy(
                socketContext.ReadBuffer, socketContext.Offset,
                socketContext.PayloadBuffer, socketContext.PayloadRead,
                takePayload);

            socketContext.PayloadRead += takePayload;
            socketContext.Offset += takePayload;
            socketContext.Remaining -= takePayload;

            if (socketContext.PayloadRead < socketContext.Header.PayloadSize)
                return EReadPacketReturn.NeedMoreData;
            
            socketContext.HeaderRead = 0;
            socketContext.PayloadRead = 0;
            
            if (socketContext.Remaining  > 0)
                return EReadPacketReturn.BufferDrained;
        }

        return EReadPacketReturn.PacketReady;
    }
}