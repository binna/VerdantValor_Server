using Common.Types;

namespace Common.KeyValueStore;

public interface ISessionKeyValueStore
{
    Task<bool> AddUserSessionInfoAsync(string key, UserSessionInfo value);
    Task<UserSessionInfo> GetUserSessionInfoAsync(string key);
    Task<bool> ExtendUserSessionInfoAsync(string key);
    Task<bool> RefreshServerHeartbeatAsync(string serverName, string serverIp);
    Task<bool> IsServerAliveAsync(string serverName, string serverIp);
}