using Common.Driver;
using Common.Types;

namespace Common.Web;

public sealed class RedisKeyValueStore : IKeyValueStore
{
    private readonly ICacheDriver mCoreCacheDriver;
    private readonly ICacheDriver mSessionCacheDriver;

    public RedisKeyValueStore(
        ICacheDriver coreCacheDriver,
        ICacheDriver sessionCacheDriver)
    {
        mCoreCacheDriver = coreCacheDriver;
        mSessionCacheDriver = sessionCacheDriver;
    }

    #region Core
    public Task<bool> AddHashAsync(string key, string hashField, string hashValue)
    {
        return mCoreCacheDriver.HashSetAsync(key, hashField, hashValue);
    }

    public Task<bool> AddSortedSetAsync(string key, string member, double score)
    {
        return mCoreCacheDriver.SortedSetAddAsync(key, member, score);
    }

    public async Task<RankingEntry[]> GetTopRankingByType(string key, int rank)
    {
        var entries = await mCoreCacheDriver
            .SortedSetRangeByRankWithScoresAsync(key, 0, rank - 1, ICacheDriver.EGetOrder.Descending);
        
        var rankings = new RankingEntry[entries.Length];
        
        for (var i = 0; i < entries.Length; i++)
        {
            rankings[i] = new RankingEntry(entries[i].Element, entries[i].Score);
        }

        return rankings;
    }

    public Task<long?> GetMemberRank(string key, string member)
    {
        return mCoreCacheDriver.SortedSetRankAsync(key, member, ICacheDriver.EGetOrder.Descending);
    }

    public Task<double?> GetMemberScore(string key, string member)
    {
        return mCoreCacheDriver.SortedSetScoreAsync(key, member);
    }
    #endregion
    
    #region Session
    public Task<bool> AddSessionInfoAsync(string key, string value)
    {
        return mSessionCacheDriver.StringSetAsync(key, value);
    }
    
    public async Task<string> GetSessionInfoAsync(string key)
    {
        var value = await mSessionCacheDriver.StringGetAsync(key);
        return value;
    }
    #endregion
}