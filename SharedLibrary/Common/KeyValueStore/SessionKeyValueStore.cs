using Common.Driver;

namespace Common.KeyValueStore;

public class SessionKeyValueStore : ISessionKeyValueStore
{
    private readonly ICacheDriver mCacheDriver;

    public SessionKeyValueStore(ICacheDriver cacheDriver)
    {
        mCacheDriver = cacheDriver;
    }
    
    public Task<bool> AddSessionInfoAsync(string key, string value)
    {
        return mCacheDriver.StringSetAsync(key, value);
    }
    
    public async Task<string> GetSessionInfoAsync(string key)
    {
        var value = await mCacheDriver.StringGetAsync(key);
        return value;
    }
}