using System.Collections.Concurrent;
using System.Net.Sockets;
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
    public enum BroadcastTarget
    {
        Unknown = 0,
        World,
        Party
    }
    
    public ConcurrentDictionary<TcpClient, byte> ConnectedClient { get; private set; } = [];
    public ConcurrentDictionary<ulong, Session> LoginSessions { get; private set; } = [];
    
    public ConcurrentDictionary<string, ConcurrentDictionary<ulong, byte>> World { get; private set; } = [];
    public ConcurrentDictionary<string, ConcurrentDictionary<ulong, byte>> Party { get; private set; } = [];
    
    private ISessionKeyValueStore mSessionKeyValueStore;
    // TODO DB 연결도 여기서 하고 싶은데

    public SessionManager()
    {
        mSessionKeyValueStore = 
            new SessionKeyValueStore(
                new RedisCacheDriver(
                    "localhost", 
                    $"{6379}", 
                    ShareServerConst.USER_SESSION_DB_NUM), 
                0);

        // TODO 서버 처음에 로딩하기
        World["Korea_1"] = [];
        
        // TODO 파티도 리스트 만들기
        Party["Korea_1"] = [];
        
        // TODO 서버 살아 있음을 보내는 하트비트 부분 만들기
        // TODO 로그아웃 후 파티는 나가는 것이 필요할까? 나중에 처리 필요
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
}