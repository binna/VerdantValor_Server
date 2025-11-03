using StackExchange.Redis;

namespace SharedLibrary.Database.Redis;

public sealed class RedisClient
{
    private IDatabase? mDatabase;
    private IDatabase? mSessionDatabase;
    private static readonly Lazy<RedisClient> mInstance = new(() => new RedisClient());
    public static RedisClient Instance => mInstance.Value;

    public void Init(string host, string port)
    {
        if (mDatabase != null ||  mSessionDatabase != null)
            return;
        
        var endpoint = $"{host}:{port}";
        var connection = ConnectionMultiplexer.Connect(endpoint);
        mDatabase = connection.GetDatabase(0);
        mSessionDatabase = connection.GetDatabase(1);
    }
    
    // Set 결과
    //  true: 새 멤버가 추가됨
    //  false: 이미 존재해서 갱신됨
    
    public Task<bool> AddHashAsync(string key, string hashField, string hashValue)
    {
        if (mDatabase == null)
            throw new RedisConnectionException(ConnectionFailureType.Loading, "Redis database is not initialized.");
        
        return mDatabase.HashSetAsync(key, hashField, hashValue);
    }

    public Task<bool> AddSortedSetAsync(string key, string member, double score)
    {
        if (mDatabase == null)
            throw new RedisConnectionException(ConnectionFailureType.Loading, "Redis database is not initialized.");
        
        return mDatabase.SortedSetAddAsync(key, member, score);
    }

    public Task<SortedSetEntry[]> GetTopRankingByType(string key, int rank)
    {
        if (mDatabase == null)
            throw new RedisConnectionException(ConnectionFailureType.Loading, "Redis database is not initialized.");
        
        return mDatabase.SortedSetRangeByRankWithScoresAsync(key, 0, rank - 1, Order.Descending);
    }

    public Task<long?> GetMemberRank(string key, string member)
    {
        if (mDatabase == null)
            throw new RedisConnectionException(ConnectionFailureType.Loading, "Redis database is not initialized.");
        
        return mDatabase.SortedSetRankAsync(key, member, Order.Descending);
    }

    public Task<double?> GetMemberScore(string key, string member)
    {
        if (mDatabase == null)
            throw new RedisConnectionException(ConnectionFailureType.Loading, "Redis database is not initialized.");
        
        return mDatabase.SortedSetScoreAsync(key, member);
    }

    #region Session용
    public Task<bool> AddSessionInfoAsync(string key, string value)
    {
        if (mSessionDatabase == null)
            throw new RedisConnectionException(ConnectionFailureType.Loading, "Session Redis database is not initialized.");
        
        return mSessionDatabase.StringSetAsync(key, value);
    }
    
    public Task<RedisValue> GetSessionInfoAsync(string key)
    {
        if (mSessionDatabase == null)
            throw new RedisConnectionException(ConnectionFailureType.Loading, "Session Redis database is not initialized.");
        
        return mSessionDatabase.StringGetAsync(key);
    }
    #endregion
}