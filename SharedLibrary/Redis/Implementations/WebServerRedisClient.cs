using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Redis.Interfaces;
using StackExchange.Redis;

namespace Redis.Implementations;

public sealed class WebServerRedisClient : IWebServerRedisClient
{
    private readonly IDatabase mCoreDatabase;
    private readonly IDatabase mSessionDatabase;

    public WebServerRedisClient(
        IConfiguration configuration,
        ILogger<IWebServerRedisClient> logger)
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
            mCoreDatabase = connection.GetDatabase(0);
            mSessionDatabase = connection.GetDatabase(1);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Redis connection Fail. {@context}", new { host, port });
            Environment.Exit(1);
        }
        
        logger.LogInformation("Redis connection success. {@context}", new { host, port });
    }

    #region Core
    public Task<bool> AddHashAsync(string key, string hashField, string hashValue)
    {
        return mCoreDatabase.HashSetAsync(key, hashField, hashValue);
    }

    public Task<bool> AddSortedSetAsync(string key, string member, double score)
    {
        return mCoreDatabase.SortedSetAddAsync(key, member, score);
    }

    public Task<SortedSetEntry[]> GetTopRankingByType(string key, int rank)
    {
        return mCoreDatabase.SortedSetRangeByRankWithScoresAsync(key, 0, rank - 1, Order.Descending);
    }

    public Task<long?> GetMemberRank(string key, string member)
    {
        return mCoreDatabase.SortedSetRankAsync(key, member, Order.Descending);
    }

    public Task<double?> GetMemberScore(string key, string member)
    {
        return mCoreDatabase.SortedSetScoreAsync(key, member);
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