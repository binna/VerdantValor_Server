using System.Text.Json;
using Common.Driver;
using Common.Types;

namespace Common.KeyValueStore;

public class SessionKeyValueStore : ISessionKeyValueStore
{
    private readonly ICacheDriver mCacheDriver;
    private readonly TimeSpan? mSessionExpiry;

    public SessionKeyValueStore(ICacheDriver cacheDriver, long expiryMs)
    {
        mCacheDriver = cacheDriver;

        if (expiryMs == 0)
        {
            mSessionExpiry = null;
            return;
        }

        mSessionExpiry = TimeSpan.FromMilliseconds(expiryMs);
    }
    
    public Task<bool> AddUserSessionInfoAsync(string key, UserSessionInfo value)
    {
        // TODO 인코딩을 해야할 수도
        //  서버는 3분을 / 클라는 1분마다 -> 최소 2번은 가능
        return mCacheDriver.StringSetAsync(key, JsonSerializer.Serialize(value), mSessionExpiry);
    }
    
    public async Task<UserSessionInfo> GetUserSessionInfoAsync(string key)
    {
        // TODO 만약 인코딩을 했다면 디코딩을 해야할 수도
        var json = await mCacheDriver.StringGetAsync(key);
        return JsonSerializer.Deserialize<UserSessionInfo>(json);
    }

    public async Task<bool> ExtendUserSessionInfoAsync(string key)
    {
        return await mCacheDriver.KeyExpireAsync(key, mSessionExpiry);
    }
}