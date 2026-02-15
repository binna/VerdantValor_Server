using StackExchange.Redis;

namespace Redis.Interfaces;

public interface IWebServerRedisClient
{
    #region Core
    public Task<bool> AddHashAsync(string key, string hashField, string hashValue);
    public Task<bool> AddSortedSetAsync(string key, string member, double score);
    public Task<SortedSetEntry[]> GetTopRankingByType(string key, int rank);
    public Task<long?> GetMemberRank(string key, string member);
    public Task<double?> GetMemberScore(string key, string member);
    #endregion
    
    #region Session
    public Task<bool> AddSessionInfoAsync(string key, string value);
    public Task<RedisValue> GetSessionInfoAsync(string key);
    #endregion
}