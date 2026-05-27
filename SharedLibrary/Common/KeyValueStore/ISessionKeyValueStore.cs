namespace Common.KeyValueStore;

public interface ISessionKeyValueStore
{
    public Task<bool> AddSessionInfoAsync(string key, string value);
    public Task<string> GetSessionInfoAsync(string key);
}