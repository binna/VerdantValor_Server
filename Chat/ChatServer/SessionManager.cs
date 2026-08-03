using System.Collections.Concurrent;
using System.Net.Sockets;
using Ado.Daos;
using Common.KeyValueStore;
using Common.Types;
using Shared.Types;
using Tcp;

namespace ChatServer;

// Manager는 상태와 객체를 관리하고, Service는 비즈니스 로직을 수행한다.
//  Manager = 관리한다 (Manage)
//  Service = 일을 한다 (Do)
public class SessionManager
{
    // TODO Max 패킷을 공통 두기
    //  매번 계산하기 힘듦
    ///////////////////////////////////////////////////////////////////////////////////////////////////////
    // TODO
    //  금칙어 추가
    // TODO
    //  DB Distory 중에서 처리를 안한다는 건가 이런 방법을 고민해기
    //  플로우차트에 대한 느낌, 어디로 진행하고 어디로 하고 
    //  멀티 인스턴스 이 정보를 다른 애들에게도 알리고,,
    /////////////////////////////////////////////////////////////////////////////////////////////////////
    private ConcurrentDictionary<TcpClient, byte> mConnectedClient = [];
    private ConcurrentDictionary<ulong, Session> mLoginSessions = [];

    private readonly IChatPartyDao mChatPartyDao;
    private readonly ISessionKeyValueStore mSessionKeyValueStore;
    private readonly CancellationToken mToken;
    
    public SessionManager(
        IChatPartyDao chatPartyDao, 
        ISessionKeyValueStore sessionKeyValueStore,
        CancellationToken token)
    {
        mChatPartyDao = chatPartyDao;
        mSessionKeyValueStore = sessionKeyValueStore;
        mToken = token;
    }

    public IEnumerable<TcpClient> GetConnectedClients() => mConnectedClient.Keys;
    public IEnumerable<Session> GetLoginSessions() => mLoginSessions.Values;
    public bool AddConnectedClient(TcpClient tcpClient) => mConnectedClient.TryAdd(tcpClient, 0);
    public bool RemoveConnectedClient(TcpClient client) => mConnectedClient.TryRemove(client, out _);
    public bool RemoveLoginSession(ulong userId) => mLoginSessions.TryRemove(userId, out _);
    public bool TryGetLoginSession(ulong userId, out Session? session) => mLoginSessions.TryGetValue(userId, out session);

    public async Task<EResponseResult> LoginAsync(SocketContext socketContext, UserSessionInfo userSessionInfo)
    {
        if (!mConnectedClient.TryRemove(socketContext.Client, out _))
            return EResponseResult.ConnectionNotFound;

        var bAdded = await mSessionKeyValueStore.AddUserSessionInfoAsync($"{socketContext.Session.UserId}", userSessionInfo);
        if (!bAdded) 
            return EResponseResult.RedisError;
        
        mLoginSessions[socketContext.Session.UserId] = socketContext.Session;
        return EResponseResult.Success;
    }
}
