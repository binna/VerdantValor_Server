using System.Text.Json;
using Common.Driver;
using Common.Types;

namespace Common.KeyValueStore;

public class SessionKeyValueStore : ISessionKeyValueStore
{
    private readonly ICacheDriver mCacheDriver;

    public SessionKeyValueStore(ICacheDriver cacheDriver)
    {
        mCacheDriver = cacheDriver;
    }
    
    public Task<bool> AddUserSessionInfoAsync(string key, UserSessionInfo value)
    {
        // TODO 인코딩을 해야할 수도
        return mCacheDriver.StringSetAsync(key, JsonSerializer.Serialize(value));
    }
    
    public async Task<UserSessionInfo> GetUserSessionInfoAsync(string key)
    {
        // TODO 만약 인코딩을 했다면 디코딩을 해야할 수도
        var json = await mCacheDriver.StringGetAsync(key);
        return JsonSerializer.Deserialize<UserSessionInfo>(json);
    }
}