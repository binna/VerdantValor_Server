using StackExchange.Redis;

namespace Redis;

public interface IRedisClient
{
    // Set 결과
    //  true: 새 멤버가 추가됨
    //  false: 이미 존재해서 갱신됨

    #region default
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