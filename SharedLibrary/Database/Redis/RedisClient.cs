using StackExchange.Redis;

namespace SharedLibrary.Database.Redis;

public sealed class RedisClient
{
    private IDatabase? mDatabase;
    private static readonly Lazy<RedisClient> mInstance = new(() => new RedisClient());
    public static RedisClient Instance => mInstance.Value;

    public void Init(string host, string port)
    {
        if (mDatabase != null)
            return;
        
        var endpoint = $"{host}:{port}";
        var connection = ConnectionMultiplexer.Connect(endpoint);
        mDatabase = connection.GetDatabase(0);
    }
    
    // Set 결과
    //  true: 새 멤버가 추가됨
    //  false: 이미 존재해서 갱신됨
    
    public Task<bool> AddStringAsync(string key, string value)
    {
        if (mDatabase == null)
            throw new RedisConnectionException(ConnectionFailureType.Loading, "Redis database is not initialized.");
        
        return mDatabase.StringSetAsync(key, value);
    }

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
}