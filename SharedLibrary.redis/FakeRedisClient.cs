using StackExchange.Redis;

namespace SharedLibrary.Redis;

// 모킹에서 쓰려고 상속 가능하게 구현
public class FakeRedisClient : IRedisClient 
{
    private readonly IDatabase mDatabase;
    private readonly IDatabase mSessionDatabase;

    public FakeRedisClient(string host, string port, int defaultDb, int sessionDb)
    {
        var endpoint = $"{host}:{port}";
        var connection = ConnectionMultiplexer.Connect(endpoint);
        mDatabase = connection.GetDatabase(defaultDb);
        mSessionDatabase = connection.GetDatabase(sessionDb);
    }
    
    // Set 결과
    //  true: 새 멤버가 추가됨
    //  false: 이미 존재해서 갱신됨
    
    public Task<bool> AddHashAsync(string key, string hashField, string hashValue)
    {
        return mDatabase.HashSetAsync(key, hashField, hashValue);
    }

    public Task<bool> AddSortedSetAsync(string key, string member, double score)
    {
        return mDatabase.SortedSetAddAsync(key, member, score);
    }

    public Task<SortedSetEntry[]> GetTopRankingByType(string key, int rank)
    {
        return mDatabase.SortedSetRangeByRankWithScoresAsync(key, 0, rank - 1, Order.Descending);
    }

    public Task<long?> GetMemberRank(string key, string member)
    {
        return mDatabase.SortedSetRankAsync(key, member, Order.Descending);
    }

    public Task<double?> GetMemberScore(string key, string member)
    {
        return mDatabase.SortedSetScoreAsync(key, member);
    }

    #region Session용
    public Task<bool> AddSessionInfoAsync(string key, string value)
    {
        return mSessionDatabase.StringSetAsync(key, value);
    }
    
    public Task<RedisValue> GetSessionInfoAsync(string key)
    {
        return mSessionDatabase.StringGetAsync(key);
    }
    #endregion
}