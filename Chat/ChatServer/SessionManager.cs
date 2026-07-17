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
    
    private ISessionKeyValueStore mSessionKeyValueStore;
    private ChatPartyDao mChatPartyDao;
    
    // TODO 로그아웃 후 파티는 나가는 것이 필요할까? 나중에 처리 필요
    // TODO 서버 살아 있음을 보내는 하트비트 부분 만들기

    public SessionManager()
    {
        mSessionKeyValueStore = 
            new SessionKeyValueStore(
                new RedisCacheDriver(
                    "localhost", 
                    $"{6379}", 
                    ShareServerConst.USER_SESSION_DB_NUM), 
                0);

        mChatPartyDao =
            new ChatPartyDao(
                new DbFactory(
                    "Server=localhost;Database=VerdantValor;Uid=root;Pwd=940404;"));
    }

    public async Task Init()
    {
        var partyIds = await mChatPartyDao.FindAllPartyIdAsync();
        foreach (var partyId in partyIds)
        {
            Party.TryAdd(partyId, new ConcurrentDictionary<ulong, byte>());
        }

        // TODO 서버 처음에 로딩하기
        World["Korea_1"] = [];
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
    
    public async Task StartHeartbeatLoopAsync(string serverName, string serverIp, CancellationTokenSource cts)
    {
        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                var success = await mSessionKeyValueStore.RefreshServerHeartbeatAsync(serverName, serverIp);
                if (!success)
                    Console.WriteLine($"[Error] {serverName} Heartbeat Failed - {serverIp}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Heartbeat Exception: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromMinutes(ShareServerConst.HEARTBEAT_MINUTES), cts.Token);
        }
    }
}