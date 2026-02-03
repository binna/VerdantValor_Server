using System.Net.Sockets;
using MemoryPack;
using Protocol.Chat.Frames;
using Shared.Constants;
using Shared.Types;

namespace Tcp;

public abstract class NetworkSocket
{
    private enum ReadPacketReturn
    {
        NeedMoreData,
        PacketReady,
        BufferDrained
    }
    
    protected readonly CancellationTokenSource mCts;
    
    private readonly 
        Dictionary<EPacket, Func<SocketContext, CancellationToken, Task>> mPacketHandlers;
    
    public NetworkSocket(
        Dictionary<EPacket, Func<SocketContext, CancellationToken, Task>> packetHandlers, 
        CancellationToken cancellationToken = default)
    {
        mPacketHandlers = packetHandlers;
        mCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public abstract Task StartAsync();

    protected async Task HandleClientReadAsync(SocketContext socketContext, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var read = await socketContext.Stream.ReadAsync(
                socketContext.ReadBuffer, cancellationToken);
        
            if (read == 0)
            {
                Console.WriteLine("disconnected");
                return;
            }
            
            socketContext.Offset = 0;
            socketContext.Remaining = read;

            while (!cancellationToken.IsCancellationRequested)
            {
                var result = ReadPacket(socketContext);

                if (result == ReadPacketReturn.NeedMoreData)
                    break;

                if (Enum.IsDefined(typeof(EPacket), socketContext.Header.PacketType))
                {
                    await mPacketHandlers[socketContext.Header.PacketType](socketContext, cancellationToken);
                }

                if (result == ReadPacketReturn.PacketReady)
                    break;
            }
        }
    }

    protected static async Task WritePacket<T>(NetworkStream stream, Packet<T> message, CancellationToken cancellationToken) where T : struct, IPacketBody
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
}