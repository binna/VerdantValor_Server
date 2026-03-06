namespace Common.Concurrency.Driver;

public interface ICacheDriver
{
    public enum ESetCondition
    {
        None = 0,
        Exists = 1,
        NotExists = 2,
    }
    
    Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null, ESetCondition condition = ESetCondition.None, CancellationToken token = default);
    Task<string> ScriptEvaluateAsync(string script, string[] keys, string[] values, CancellationToken token = default);
}