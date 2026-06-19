using Common.Types;

namespace Common.KeyValueStore;

public interface ISessionKeyValueStore
{
    public Task<bool> AddUserSessionInfoAsync(string key, UserSessionInfo value);
    public Task<UserSessionInfo> GetUserSessionInfoAsync(string key);
}