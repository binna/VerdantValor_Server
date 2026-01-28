using VerdantValorShared.Common.ChatServer;
using VerdantValorShared.Packet;

namespace Tcp;

public abstract class SocketServer
{
    protected readonly Dictionary<AppEnum.PacketType, Func<SocketContext, CancellationToken, Task>> mPacketHandlers;
    protected readonly CancellationTokenSource mCts;
    
    protected enum ReadPacketReturn
    {
        NeedMoreData,
        PacketReady,
        BufferDrained
    }
    
    public SocketServer(
        Dictionary<AppEnum.PacketType, Func<SocketContext, CancellationToken, Task>> packetHandlers,
        CancellationToken cancellationToken = default)
    {
        mPacketHandlers = packetHandlers;
        mCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public abstract Task StartAsync();

    protected static ReadPacketReturn ReadPacket(SocketContext socketContext)
    {
        while (socketContext.Remaining > 0)
        {
            if (socketContext.HeaderRead < Header.HEADER_SIZE)
            {
                var needHeader = Header.HEADER_SIZE - socketContext.HeaderRead;
                var takeHeader = Math.Min(needHeader, socketContext.Remaining);

                Buffer.BlockCopy(
                    socketContext.ReadBuffer, socketContext.Offset,
                    socketContext.HeaderBuffer, socketContext.HeaderRead,
                    takeHeader);

                socketContext.HeaderRead += takeHeader;
                socketContext.Offset += takeHeader;
                socketContext.Remaining -= takeHeader;

                if (socketContext.HeaderRead < Header.HEADER_SIZE)
                    return ReadPacketReturn.NeedMoreData;

                var beforePayloadLength = socketContext.Header.PayloadLength;

                socketContext.Header.Parse(socketContext.HeaderBuffer);

                if (beforePayloadLength < socketContext.Header.PayloadLength)
                    socketContext.PayloadBuffer = new byte[socketContext.Header.PayloadLength];
            }

            var needPayLoad = socketContext.Header.PayloadLength - socketContext.PayloadRead;
            var takePayLoad = Math.Min(needPayLoad, socketContext.Remaining);

            Buffer.BlockCopy(
                socketContext.ReadBuffer, socketContext.Offset,
                socketContext.PayloadBuffer, socketContext.PayloadRead,
                takePayLoad);

            socketContext.PayloadRead += takePayLoad;
            socketContext.Offset += takePayLoad;
            socketContext.Remaining -= takePayLoad;

            if (socketContext.PayloadRead < socketContext.Header.PayloadLength)
                return ReadPacketReturn.NeedMoreData;
            
            socketContext.HeaderRead = 0;
            socketContext.PayloadRead = 0;
            
            if (socketContext.Remaining  > 0)
                return ReadPacketReturn.BufferDrained;
        }

        return ReadPacketReturn.PacketReady;
    }
}