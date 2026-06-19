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
    
    public Task<bool> AddSessionInfoAsync(string key, UserSessionInfo value)
    {
        return mCacheDriver.StringSetAsync(key, JsonSerializer.Serialize(value));
    }
    
    public async Task<string> GetSessionInfoAsync(string key)
    {
        var value = await mCacheDriver.StringGetAsync(key);
        return value;
    }
}