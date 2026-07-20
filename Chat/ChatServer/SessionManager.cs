using System.Collections.Concurrent;
using System.Net.Sockets;
using Ado;
using Ado.Daos;
using Common;
using Common.KeyValueStore;
using Common.Types;
using Redis;
using Tcp;

namespace ChatServer;

// Manager는 상태와 객체를 관리하고, Service는 비즈니스 로직을 수행한다.
//  Manager = 관리한다 (Manage)
//  Service = 일을 한다 (Do)
public class SessionManager
{
    public ConcurrentDictionary<TcpClient, byte> ConnectedClient { get; private set; } = [];
    public ConcurrentDictionary<ulong, Session> LoginSessions { get; private set; } = [];

    public ConcurrentDictionary<string, ConcurrentDictionary<ulong, byte>> World { get; private set; }
    public ConcurrentDictionary<string, ConcurrentDictionary<ulong, byte>> Party { get; private set; }
    
    private ChatPartyDao mChatPartyDao;
    private ISessionKeyValueStore mSessionKeyValueStore;
    
    // TODO
    //  로그아웃 후 파티는 나가는 것이 필요할까? 나중에 처리 필요
    // TODO
    //  금칙어 추가
    // TODO
    //  DB Distory 중에서 처리를 안한다는 건가 이런 방법을 고민해기
    //  플로우차트에 대한 느낌, 어디로 진행하고 어디로 하고 
    //  멀티 인스턴스 이 정보를 다른 애들에게도 알리고,,

    public SessionManager(string dbUrl, string redisHost, string redisPort)
    {
        mChatPartyDao =
            new ChatPartyDao(
                new DbFactory(dbUrl));
        
        mSessionKeyValueStore = 
            new SessionKeyValueStore(
                new RedisCacheDriver(
                    redisHost, 
                    redisPort, 
                    ShareServerConst.USER_SESSION_DB_NUM), 
                0);
    }

    public async Task Init()
    {
        World = [];
        Party = [];
        
        // TODO 서버 처음에 로딩하기
        World["Korea_1"] = [];
        
        var partyIds = await mChatPartyDao.FindAllPartyIdAsync();
        foreach (var partyId in partyIds)
        {
            Party.TryAdd(partyId, new ConcurrentDictionary<ulong, byte>());
        }
    }

    public bool AddUserToWorld(string worldName, ulong userId)
    {
        return World[worldName].TryAdd(userId, 0);
    }

    public async Task<UserSessionInfo> GetUserSessionInfoAsync(string userId)
    {
        return await mSessionKeyValueStore.GetUserSessionInfoAsync(userId);
    }

    public async Task<bool> AddUserSessionInfoAsync(string userId, UserSessionInfo userSessionInfo)
    {
        return await mSessionKeyValueStore.AddUserSessionInfoAsync(userId, userSessionInfo);
    }
    
    public async Task StartHeartbeatLoopAsync(string serverName, string serverIp, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var success = await mSessionKeyValueStore.RefreshServerHeartbeatAsync(serverName, serverIp);
                if (!success)
                    Console.WriteLine($"[Error] {serverName} Heartbeat Failed - {serverIp}");
                
                await Task.Delay(TimeSpan.FromMinutes(ShareServerConst.HEARTBEAT_MINUTES), token);
            }
            catch (OperationCanceledException ex)
            {
                // 취소 시그널은 의도된 종료이므로 에러가 아님, 루프를 빠져나감
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Heartbeat Exception: {ex.Message}");
            }
        }
    }
}
