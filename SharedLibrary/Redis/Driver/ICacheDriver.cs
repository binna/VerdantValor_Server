using StackExchange.Redis;

namespace Redis.Driver;

public interface ICacheDriver
{
    public enum ESetCondition
    {
        None = 0,
        Exists = 1,
        NotExists = 2,
    }
    
    Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null, ESetCondition condition = ESetCondition.None);
    Task<RedisResult> ScriptEvaluateAsync(string script, string[] keys, string[] values);
}