using Common.Driver;
using Common.Types;

namespace Common.KeyValueStore;

public class WebKeyValueStore : IWebKeyValueStore
{
    private readonly ICacheDriver mCacheDriver;

    public WebKeyValueStore(ICacheDriver cacheDriver)
    {
        mCacheDriver = cacheDriver;
    }
    
    public Task<bool> AddHashAsync(string key, string hashField, string hashValue)
    {
        return mCacheDriver.HashSetAsync(key, hashField, hashValue);
    }

    public Task<bool> AddSortedSetAsync(string key, string member, double score)
    {
        return mCacheDriver.SortedSetAddAsync(key, member, score);
    }

    public async Task<RankingEntry[]> GetTopRankingByType(string key, int rank)
    {
        var entries = await mCacheDriver
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
        return mCacheDriver.SortedSetRankAsync(key, member, ICacheDriver.EGetOrder.Descending);
    }

    public Task<double?> GetMemberScore(string key, string member)
    {
        return mCacheDriver.SortedSetScoreAsync(key, member);
    }
}