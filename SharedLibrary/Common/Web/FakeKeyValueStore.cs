using System.Collections.Concurrent;
using Common.Types;

namespace Common.Web;

public sealed class FakeKeyValueStore : IKeyValueStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> mCoreHash = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, double>> mCoreSortedSet = new();
    
    private readonly ConcurrentDictionary<string, string> mSessionString = new();
    
    // Set 결과
    //  true: 새 멤버가 추가됨
    //  false: 이미 존재해서 갱신됨
    
    public Task<bool> AddHashAsync(string key, string hashField, string hashValue)
    {
        var hash = mCoreHash
            .GetOrAdd(key, _ => new ConcurrentDictionary<string, string>());
        
        if (hash.TryAdd(hashField, hashValue))
            return Task.FromResult(true);
        
        hash[hashField] = hashValue;
        return Task.FromResult(false);
    }

    public Task<bool> AddSortedSetAsync(string key, string member, double score)
    {
        var sortedSet = mCoreSortedSet
            .GetOrAdd(key, _ => new ConcurrentDictionary<string, double>());
        
        if (sortedSet.TryAdd(member, score))
            return Task.FromResult(true);
        
        sortedSet[member] = score;
        return Task.FromResult(false);
    }

    public Task<RankingEntry[]> GetTopRankingByType(string key, int rank)
    {
        if (!mCoreSortedSet.TryGetValue(key, out var sortedSet))
            return Task.FromResult(Array.Empty<RankingEntry>());
        
        var result = sortedSet
            .OrderByDescending(x => x.Value)
            .Take(rank)
            .Select(x => new RankingEntry(x.Key, x.Value))
            .ToArray();
        
        return Task.FromResult(result);
    }

    public Task<long?> GetMemberRank(string key, string member)
    {
        if (!mCoreSortedSet.TryGetValue(key, out var sortedSet))
            return Task.FromResult<long?>(null);
        
        var sortedSetByDesc = sortedSet
            .OrderByDescending(x => x.Value)
            .Select((entry, rank) => new { entry.Key, rank });
        
        var foundData = sortedSetByDesc
            .FirstOrDefault(x => x.Key == member);
        
        return Task.FromResult<long?>(foundData?.rank);
    }

    public Task<double?> GetMemberScore(string key, string member)
    {
        if (!mCoreSortedSet.TryGetValue(key, out var sortedSet))
            return Task.FromResult<double?>(null);
        
        var result = sortedSet
            .FirstOrDefault(x => x.Key == member).Value;
        
        return Task.FromResult<double?>(result);
    }

    public Task<bool> AddSessionInfoAsync(string key, string value)
    {
        if (mSessionString.TryAdd(key, value))
            return Task.FromResult(true);
        
        mSessionString[key] = value;
        return Task.FromResult(false);
    }

    public Task<string> GetSessionInfoAsync(string key)
    {
        var value = mSessionString.GetValueOrDefault(key, "");
        return Task.FromResult(value);
    }
}