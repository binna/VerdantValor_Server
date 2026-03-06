using Common.Types;
using Common.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Redis;

// TODO 드라이브 느낌 살려보는 것 좀 더 고민 필요

public sealed class RedisKeyValueStore : IKeyValueStore
{
    private readonly IDatabase mCoreDatabase;
    private readonly IDatabase mSessionDatabase;

    public RedisKeyValueStore(
        IConfiguration configuration,
        ILogger<IKeyValueStore> logger)
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

    public async Task<RankingEntry[]> GetTopRankingByType(string key, int rank)
    {
        var entries = await mCoreDatabase
            .SortedSetRangeByRankWithScoresAsync(key, 0, rank - 1, Order.Descending);
        
        var rankings = new RankingEntry[entries.Length];
        
        for (var i = 0; i < entries.Length; i++)
        {
            rankings[i] = new RankingEntry(entries[i].Element.ToString(), entries[i].Score);
        }

        return rankings;
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
    
    public async Task<string> GetSessionInfoAsync(string key)
    {
        var value = await mSessionDatabase.StringGetAsync(key);
        return value.ToString();
    }
    #endregion
}