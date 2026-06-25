using System.Collections.Concurrent;
using System.Net.Sockets;
using Common;
using Common.KeyValueStore;
using Common.Types;
using Redis;
using Tcp;

namespace ChatServer;

public class SessionManager
{
    // TODO 나중에 세션 관리하는 매니저를 만들 예정
    //  Manager는 상태와 객체를 관리하고, Service는 비즈니스 로직을 수행한다.
    //  Manager = 관리한다 (Manage)
    //  Service = 일을 한다 (Do)
    
    // TODO 연결된 세션, 그리고 로그인 성공한 세션 나누기
    //  연결된 세션은 로그인을 뭘하든지 리스폰 날리기
    public ConcurrentDictionary<TcpClient, byte> ConnectedClient { get; set; } = [];
    public ConcurrentDictionary<ulong, Session> LoginSessions { get; set; } = [];
    
    private ISessionKeyValueStore mSessionKeyValueStore;

    public SessionManager()
    {
        mSessionKeyValueStore = 
            new SessionKeyValueStore(
                new RedisCacheDriver(
                    "localhost", 
                    $"{6379}", 
                    ShareServerConst.USER_SESSION_DB_NUM), 
                0);
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