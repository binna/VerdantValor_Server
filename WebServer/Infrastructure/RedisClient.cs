using StackExchange.Redis;

namespace WebServer.Infrastructure;

public class RedisClient
{
    private readonly ILogger<RedisClient> mLogger;
    private readonly IDatabase mDatabase;

    public RedisClient(
        ILogger<RedisClient> logger, 
        IConfiguration configuration)
    {
        mLogger = logger;

        var host = configuration["DB:Redis:Host"];
        var port = configuration["DB:Redis:Port"];

        if (host == null || port == null)
        {
            mLogger.LogCritical("Redis configuration is missing required fields");
            Environment.Exit(1);
        }

        var endpoint = $"{host}:{port}";
        
        try
        {
            var connection = ConnectionMultiplexer.Connect(endpoint);
            mDatabase = connection.GetDatabase(0);
        }
        catch (Exception ex)
        {
            mLogger.LogCritical("Redis Connection Fail");
            Environment.Exit(1);
        }
    }
    
    public Task<bool> AddStringAsync(string key, string value)
    {
        return mDatabase.StringSetAsync(key, value);
    }

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
}