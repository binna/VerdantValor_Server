using StackExchange.Redis;

namespace Redis.Driver;

public class RedisCacheDriver : ICacheDriver, IDisposable
{
    private readonly ConnectionMultiplexer mConnection;
    private readonly IDatabase mDatabase;
    
    public RedisCacheDriver(string host, string port, int db)
    {
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(port))
            throw new ArgumentException("Host and port must not be null or empty.");
        
        var endpoint = $"{host}:{port}";
        
        mConnection = ConnectionMultiplexer.Connect(endpoint);
        mDatabase = mConnection.GetDatabase(db);
    }

    public void Dispose()
    {
        mConnection.Dispose();
    }

    public Task<bool> StringSetAsync(
        string key, 
        string value, 
        TimeSpan? expiry = null, 
        ICacheDriver.ESetCondition condition = ICacheDriver.ESetCondition.None,
        CancellationToken token = default)
    {
        switch (condition)
        {
            case ICacheDriver.ESetCondition.Exists:
                return mDatabase.StringSetAsync(key, value, expiry, When.Exists);
            case ICacheDriver.ESetCondition.NotExists:
                return mDatabase.StringSetAsync(key, value, expiry, When.NotExists);
            case ICacheDriver.ESetCondition.None:
            default:
                return mDatabase.StringSetAsync(key, value, expiry, When.Always);
        }
    }

    public async Task<string> ScriptEvaluateAsync(
        string script, 
        string[] keys, 
        string[] values, 
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(script))
            throw new ArgumentException("script must not be empty.");
        
        var redisKeys = new RedisKey[keys.Length];
        var redisValues = new RedisValue[values.Length];
        
        for (var i = 0; i <  keys.Length; i++)
        {
            redisKeys[i] = keys[i];
        }
        
        for (var i = 0; i <  values.Length; i++)
        {
            redisValues[i] = values[i];
        }

        var response = await mDatabase.ScriptEvaluateAsync(script, redisKeys, redisValues);
        
        return response.ToString();
    }
}