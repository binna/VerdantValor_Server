using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MemoryPack;
using Protocol.Chat.Frames;
using Protocol.Chat.Payloads;
using Shared.Constants;
using Shared.Types;
using Tcp;

namespace ChatServer.Client;

public class ChatSocketClient
{
    private enum EReadPacketReturn
    {
        NeedMoreData,
        PacketReady,
        BufferDrained
    }
    
    // TODO RequestId를 혹시 몰라 추가했음
    //  반드시 req, res 1:1 관계이어야 한다면 필요할 것 같아서
    //  만약 필요없다면 패킷 구조 자체를 수정해야함
    
    // TODO 패킷의 Size 검증은 지금은 Readyonly 구조체로 해서 지금은 알아서 생성하겠끔 했음
    //  이렇게 함으로써 잘못된 길이값에 대한 대응을 했는데, 혹시 더 대응책이 필요한지 고민해보기
    
    private readonly Dictionary<EPacket, Func<SocketContext, CancellationToken, Task>> mPacketHandlers;

    private readonly CancellationToken mToken;
    private readonly TcpClient mClient;
    private readonly SocketContext mSocketContext;
    
    public ChatSocketClient(CancellationToken token = default) 
    {
        mPacketHandlers = new Dictionary<EPacket, Func<SocketContext, CancellationToken, Task>>
        {
            // [EPacket.Login] = HandleLoginAsync,
            // [EPacket.EnterWorld] = HandleEnterWorldAsync,
            // [EPacket.CreateParty] = HandleCreatePartyAsync,
            // [EPacket.DeleteParty] = HandleDeletePartyAsync,
            // [EPacket.EnterParty] = HandleEnterPartyAsync,
            // [EPacket.ExitParty] = HandleExitPartyAsync,
            //[EPacket.SendMessage] = HandleSendMessageAsync,
        };
            
        mToken = token;
        mClient = new TcpClient();
        mSocketContext = new SocketContext(mClient);
    }

    #region 패킷 전송
    public async Task LoginAsync(ulong userId, string sessionId)
    {
        Console.WriteLine(mSocketContext.Session.SessionId);
        var packet =
            new Packet<LoginReq>(
                EPacket.Login,
                new LoginReq
                {
                    SessionId = sessionId,
                    UserId = userId
                });
        await WritePacket(mClient.GetStream(), packet, mToken);
    }

    public async Task EnterWorldAsync(string worldName)
    {
        var packet =
            new Packet<EnterWorldReq>(
                EPacket.EnterWorld,
                new EnterWorldReq
                {
                    WorldName = worldName
                });
        await WritePacket(mClient.GetStream(), packet, mToken);
    }

    public async Task CreatePartyAsync(string partyId)
    {
        var packet =
            new Packet<CreatePartyReq>(
                EPacket.CreateParty,
                new CreatePartyReq
                {
                    PartyId = partyId
                });
        await WritePacket(mClient.GetStream(), packet, mToken);
    }

    public async Task DeletePartyAsync(string partyId)
    {
        var packet =
            new Packet<DeletePartyReq>(
                EPacket.DeleteParty,
                new DeletePartyReq
                {
                    PartyId = partyId
                });
        await WritePacket(mClient.GetStream(), packet, mToken);
    }

    public async Task EnterPartyAsync()
    {
        var packet =
            new Packet<EnterPartyReq>(
                EPacket.EnterParty,
                new EnterPartyReq());
        await WritePacket(mClient.GetStream(), packet, mToken);
    }

    public async Task ExitPartyAsync()
    {
        var packet =
            new Packet<ExitPartyReq>(
                EPacket.ExitParty,
                new ExitPartyReq());
        await WritePacket(mClient.GetStream(), packet, mToken);
    }

    public async Task WorldSendMessage(string message)
    {
        var packet =
            new Packet<SendGroupMessageReq>(
                EPacket.SendMessage,
                new SendGroupMessageReq
                {
                    Kind = new MessageKind
                    {
                        Id = $"{Guid.NewGuid()}",
                        Category = EMessageCategory.Chat,
                        Type = EMessageType.World
                    },
                    Content = message,
                    CreatedAt = DateTime.Now
                });
        await WritePacket(mClient.GetStream(), packet, mToken);
    }
    
    // TODO message 사이즈 규격 정하기, Id 처리(개수랑 서버 소속 밝히겠끔 바꾸기)

    public async Task PartySendMessage(string message)
    {
        var packet =
            new Packet<SendGroupMessageReq>(
                EPacket.SendMessage,
                new SendGroupMessageReq
                {
                    Kind = new MessageKind
                    {
                        Id = $"{Guid.NewGuid()}",
                        Category = EMessageCategory.Chat,
                        Type = EMessageType.Party
                    },
                    Content = message,
                    CreatedAt = DateTime.Now
                });
        await WritePacket(mClient.GetStream(), packet, mToken);
    }
    
    public async Task DirectSendMessage(ulong userId, string nickname, ulong receiverUserId, string message)
    {
        var packet =
            new Packet<SendDirectMessageReq>(
                EPacket.SendMessage,
                new SendDirectMessageReq
                {
                    Kind = new MessageKind
                    {
                        Id = $"{Guid.NewGuid()}",
                        Category = EMessageCategory.Chat,
                        Type = EMessageType.Direct
                    },
                    SenderUserId = userId,
                    SenderUserNickname = nickname,
                    ReceiverUserId = receiverUserId,
                    Content = message,
                    CreatedAt = DateTime.Now
                });
        await WritePacket(mClient.GetStream(), packet, mToken);
    }
    #endregion

    #region 클라이언트 수신

    // TODO

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // TODO 라이브러리로 뺄 예정
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public async Task StartAsync()
    {
        await mClient.ConnectAsync(IPAddress.Loopback, 20000, mToken);
        Console.WriteLine($"[Notice] 서버에 연결되었습니다");
        
        _ = HandleClientReadAsync(mSocketContext, mToken);
    }
    
    private async Task HandleClientReadAsync(SocketContext socketContext, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var read = await socketContext.Stream.ReadAsync(
                    socketContext.ReadBuffer, token);

                if (read == 0)
                {
                    Console.WriteLine("[Info] Client Disconnected");
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
                        Console.WriteLine($"[Error] Unknown Packet Type: {packetType}");
                        break;
                    }
                    
                    if (!mPacketHandlers.TryGetValue(packetType, out var handler))
                    {
                        Console.WriteLine($"[Error] No Handler Registered For Packet Type: {packetType}");
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
            Console.WriteLine($"[Error] HandleClientReadAsync Error - {ex.Message}");
        }
        finally
        {
            // 정상적으로 통신 중일 때는 이 메서드가 끝나지 않는다.
            // 즉 finally에 도달했다는 것 자체가 연결이 끊겼음을 의미한다.
            await DisconnectClientAsync(socketContext);
        }   
    }
    
    private static Task DisconnectClientAsync(SocketContext socketContext)
    {
        socketContext.Client.Close();
        return Task.CompletedTask;
    }
    
    private static async Task WritePacket<T>(NetworkStream stream, Packet<T> message, CancellationToken cancellationToken) where T : struct, IPacketBody
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