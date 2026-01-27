using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Redis;

public sealed class ConfigRedisClient : IRedisClient
{
    private readonly IDatabase mDatabase;
    private readonly IDatabase mSessionDatabase;

    public ConfigRedisClient(
        IConfiguration configuration,
        ILogger<IRedisClient> logger)
    {
        var host = configuration["DB:Redis:Host"];
        var port = configuration["DB:Redis:Port"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(port))
        {
            logger.LogCritical("Configurations are missing required fields. {@fields}", 
                new { host, port });
            Environment.Exit(1);
        }

        try
        {
            var endpoint = $"{host}:{port}";
            var connection = ConnectionMultiplexer.Connect(endpoint);
            mDatabase = connection.GetDatabase(0);
            mSessionDatabase = connection.GetDatabase(1);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Redis connection Fail. {@context}", new { host, port });
            Environment.Exit(1);
        }
        
        logger.LogInformation("Redis connection success. {@context}", new { host, port });
    }

    #region default
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
    #endregion
    
    #region Session
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