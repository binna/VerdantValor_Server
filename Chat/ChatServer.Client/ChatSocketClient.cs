using System.Net;
using System.Net.Sockets;
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
    
    // TODO 지금은 switch문으로 했지만
    //  추후 게임 데이터 기반으로 Table 만들어서 Message 뽑아내는 방식으로 바꿀 예정
    
    // TODO 웹 서버 부분도 isSuccess, Message 제거하고 게임 데이터 Table 기반으로 할지도 고민해보기
    //  이렇게되면 웹서버 Response 용량을 절약할 수 있어 최종적으로 패킷 절약으로 비용 절감을 노릴 수 있다고 생각
    
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

    #region 클라이언트 선택 메뉴
    public async Task SendLoginAsync(ulong userId, string sessionId)
    {
        mSocketContext.SetSession($"{Guid.NewGuid()}", userId);
    
        Console.WriteLine(mSocketContext.Session.SessionId);
        var packet = 
            new Packet<LoginReq>(
                EPacket.Login, 
                new LoginReq
                {
                    SessionId = sessionId,
                    UserId = userId
                });
        await mSocketContext.Stream.WriteAsync(packet.PacketBytes, mToken);
    }
    
    // public async Task SendRoomListReqAsync()
    // {
    //     var packet = new Packet<RoomListReq>(EPacket.RoomList, new RoomListReq());
    //     await mSocketContext.Stream.WriteAsync(packet.PacketBytes, mCts.Token);
    // }
    //
    // public async Task SendCreateRoomAsync()
    // {
    //     var packet = new Packet<CreateRoomReq>(EPacket.EnterWorld, new CreateRoomReq());
    //     await mSocketContext.Stream.WriteAsync(packet.PacketBytes, mCts.Token);
    //     await HandleWriteAsync(mClient, mCts.Token);
    // }
    //
    // public async Task SendEnterRoomAsync(int roomId)
    // {
    //     var packet = new Packet<EnterRoomReq>(
    //         EPacket.EnterRoom,
    //         new EnterRoomReq
    //         {
    //             RoomId = roomId
    //         });
    //     await mSocketContext.Stream.WriteAsync(packet.PacketBytes, mCts.Token);
    //     await HandleWriteAsync(mClient, mCts.Token);
    // }
    // #endregion
    //
    // #region 패킷 핸들러 함수 모음
    // private async Task HandleLoginAsync(SocketContext socketContext, CancellationToken token)
    // {
    //     var payload = MemoryPackSerializer.Deserialize<LoginRes>(socketContext.PayloadBuffer);
    //
    //     switch ((EResponseResult)payload.Code)
    //     {
    //         case EResponseResult.Success:
    //             Console.WriteLine("[Notice] 로그인 성공");
    //             socketContext.IsLogin = true;
    //             break;
    //         default:
    //             Console.WriteLine("[Notice] 로그인 실패");
    //             Console.WriteLine($"[Notice] 처리되지 않음 {payload.Code}");
    //             socketContext.IsLogin = false;
    //             break;
    //     }
    // }
    //
    // private async Task HandleCreateRoomAsync(SocketContext socketContext, CancellationToken token)
    // {
    //     var payload = MemoryPackSerializer.Deserialize<CreateRoomRes>(socketContext.PayloadBuffer);
    //
    //     switch ((EResponseResult)payload.Code)
    //     {
    //         case EResponseResult.Success:
    //             Console.WriteLine("[Notice] 방 생성 성공, 생성한 방에 입장");
    //             break;
    //         case EResponseResult.LoginRequired:
    //             Console.WriteLine("[Notice] 로그인 필요");
    //             break;
    //         case EResponseResult.AlreadyIn:
    //             Console.WriteLine("[Notice] 이미 소속된 방 있음");
    //             break;
    //         default:
    //             Console.WriteLine($"[Notice] 처리되지 않음 {payload.Code}");
    //             break;
    //     }
    // }
    //
    // // TODO Delete Room
    //
    //
    // private async Task HandleRoomListAsync(SocketContext socketContext, CancellationToken token)
    // {
    //     var payload = MemoryPackSerializer.Deserialize<RoomListRes>(socketContext.PayloadBuffer);
    //     
    //     switch ((EResponseResult)payload.Code)
    //     {
    //         case EResponseResult.Success:
    //             Console.WriteLine("[Notice] Show Room List===");
    //             foreach (var roomId in payload.RoomIds)
    //             {
    //                 Console.WriteLine(roomId);
    //             }
    //             Console.WriteLine("==========================");
    //             break;
    //         case EResponseResult.LoginRequired:
    //             Console.WriteLine("[Notice] 로그인 필요");
    //             break;
    //         default:
    //             Console.WriteLine($"[Notice] 처리되지 않음 {payload.Code}");
    //             break;
    //     }
    // }
    //
    // private async Task HandleEnterRoomAsync(SocketContext socketContext, CancellationToken token)
    // {
    //     var payload = MemoryPackSerializer.Deserialize<EnterRoomRes>(socketContext.PayloadBuffer);
    //     
    //     switch ((EResponseResult)payload.Code)
    //     {
    //         case EResponseResult.Success:
    //             Console.WriteLine("[Notice] 방에 들어왔습니다.");
    //             break;
    //         case EResponseResult.LoginRequired:
    //             Console.WriteLine("[Notice] 로그인 필요");
    //             break;
    //         case EResponseResult.NoneSelected:
    //             Console.WriteLine("검색된 방이 없습니다.");
    //             break;
    //         case EResponseResult.AlreadyIn:
    //             Console.WriteLine("[Notice] 이미 방 안에 들어가 있습니다..");
    //             break;
    //         default:
    //             Console.WriteLine($"[Notice] 처리되지 않음 {payload.Code}");
    //             break;
    //     }
    // }
    //
    // private  async Task HandleExitRoomAsync(SocketContext socketContext, CancellationToken token)
    // {
    //     var payload = MemoryPackSerializer.Deserialize<ExitRoomRes>(socketContext.PayloadBuffer);
    //     
    //     switch ((EResponseResult)payload.Code)
    //     {
    //         case EResponseResult.Success:
    //             Console.WriteLine("[Notice] 방을 정상적으로 나갔습니다.");
    //             break;
    //         case EResponseResult.LoginRequired:
    //             Console.WriteLine("[Notice] 로그인 필요");
    //             break;
    //         case EResponseResult.AlreadyOut:
    //             Console.WriteLine("[Notice] 현재 해당 방에 참여 중이 아닙니다.");
    //             break;
    //         default:
    //             Console.WriteLine($"[Notice] 처리되지 않음 {payload.Code}");
    //             break;
    //     }
    // }
    //
    // private async Task HandleSendMessageAsync(SocketContext socketContext, CancellationToken token)
    // {
    //     var payload = MemoryPackSerializer.Deserialize<SendMessageRes>(socketContext.PayloadBuffer);
    //     Console.WriteLine($"|유저 {payload.userId} 대화||||{payload.Message}");
    // }
    //
    // private async Task HandleRoomNotificationAsync(SocketContext socketContext, CancellationToken token)
    // {
    //     var payload = MemoryPackSerializer.Deserialize<RoomNotification>(socketContext.PayloadBuffer);
    //     Console.WriteLine($"*공지사항* {payload.Notification}");
    // }
    #endregion
    
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