using System.Collections.Concurrent;
using Common.Error;
using Common.Types;

namespace Common.Driver;

// Fake cache driver
//  - TTL: 지원하지 않음
//  - ESetCondition: 지원하지 않음
//  - Script evaluation: 지원하지 않음
//
// Set 계열 반환값
//  - true  : 새 항목이 추가됨
//  - false : 이미 존재하여 값이 갱신됨

public sealed class FakeCacheDriver : ICacheDriver
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> mHash = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, double>> mSortedSet = new();
    private readonly ConcurrentDictionary<string, string> mString = new();
    
    public Task<bool> StringSetAsync(
        string key, 
        string value, 
        TimeSpan? expiry = null, 
        ICacheDriver.ESetCondition condition = ICacheDriver.ESetCondition.None,
        CancellationToken token = default)
    {
        if (mString.TryAdd(key, value))
            return Task.FromResult(true);
        
        // TTL / ESetCondition 지원하지 않음
        // 옵션을 무시하고 일반 Set 동작으로 처리
        
        mString[key] = value;
        return Task.FromResult(false);
    }

    public Task<string> StringGetAsync(string key, CancellationToken token = default)
    {
        var value = mString.GetValueOrDefault(key, "");
        return Task.FromResult(value);
    }

    public Task<bool> HashSetAsync(
        string key,
        string hashField,
        string value,
        ICacheDriver.ESetCondition condition = ICacheDriver.ESetCondition.None,
        CancellationToken token = default)
    {
        var hash = mHash
            .GetOrAdd(key, _ => new ConcurrentDictionary<string, string>());
        
        // ESetCondition 지원하지 않음
        // 조건을 무시하고 항상 Set 수행

        if (hash.TryAdd(hashField, value))
            return Task.FromResult(true);
        
        hash[hashField] = value;
        return Task.FromResult(false);
    }

    public Task<bool> SortedSetAddAsync(
        string key, 
        string member,
        double score, 
        ICacheDriver.ESetCondition condition = ICacheDriver.ESetCondition.None,
        CancellationToken token = default)
    {
        var sortedSet = mSortedSet
            .GetOrAdd(key, _ => new ConcurrentDictionary<string, double>());
        
        // ESetCondition 지원하지 않음
        // 조건 없이 Add 또는 Update 수행
    
        if (sortedSet.TryAdd(member, score))
            return Task.FromResult(true);
        
        sortedSet[member] = score;
        return Task.FromResult(false);
    }

    public Task<RankingEntry[]> SortedSetRangeByRankWithScoresAsync(
        string key,
        long start = 0,
        long stop = -1,
        ICacheDriver.EGetOrder order = ICacheDriver.EGetOrder.Ascending, 
        CancellationToken token = default)
    {
        if (!mSortedSet.TryGetValue(key, out var sortedSet))
            return Task.FromResult(Array.Empty<RankingEntry>());
        
        stop = stop == -1 ? sortedSet.Count - 1 : Math.Min(stop, sortedSet.Count - 1);
        
        if (start < 0 || start > stop)
            return Task.FromResult(Array.Empty<RankingEntry>());
        
        RankingEntry[] result;

        switch (order)
        {
            case ICacheDriver.EGetOrder.Descending:
                result = sortedSet
                    .OrderByDescending(x => x.Value)
                    .Skip((int)start)
                    .Take((int)(stop - start + 1))
                    .Select(x => new RankingEntry(x.Key, x.Value))
                    .ToArray();
                break;
            case ICacheDriver.EGetOrder.Ascending:
            default:
                result = sortedSet
                    .OrderBy(x => x.Value)
                    .Skip((int)start)
                    .Take((int)(stop - start + 1))
                    .Select(x => new RankingEntry(x.Key, x.Value))
                    .ToArray();
                break;
        }
    
        return Task.FromResult(result);
    }

    public Task<long?> SortedSetRankAsync(
        string key,
        string member,
        ICacheDriver.EGetOrder order = ICacheDriver.EGetOrder.Ascending,
        CancellationToken token = default)
    {
        if (!mSortedSet.TryGetValue(key, out var sortedSet))
            return Task.FromResult<long?>(null);
        
        (string Key, int? Rank) result;
        
        switch (order)
        {
            case ICacheDriver.EGetOrder.Descending:
                result = sortedSet
                    .OrderByDescending(x => x.Value)
                    .Select((entry, rank) => ( entry.Key, rank))
                    .FirstOrDefault(x => x.Key == member);
                break;
            case ICacheDriver.EGetOrder.Ascending:
            default:
                result = sortedSet
                    .OrderBy(x => x.Value)
                    .Select((entry, rank) => ( entry.Key, rank))
                    .FirstOrDefault(x => x.Key == member);
                break;
        }
        
        var rank = result.Key == null ? (long?)null : result.Rank;
        return Task.FromResult(rank);
    }

    public Task<double?> SortedSetScoreAsync(string key, string member, CancellationToken token = default)
    {
        if (!mSortedSet.TryGetValue(key, out var sortedSet))
            return Task.FromResult<double?>(null);
        
        var result = sortedSet
            .FirstOrDefault(x => x.Key == member).Value;
        
        return Task.FromResult<double?>(result);
    }

    public Task<string> ScriptEvaluateAsync(string script, string[] keys, string[] values, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(script))
            throw new ArgumentException(
                string.Format(ErrorMessages.MUST_NOT_BE_EMPTY, "Script"));
        
        // 스크립트 실행 지원하지 않음
        // Fake driver에서는 빈 결과 반환

        return Task.FromResult("");
    }
}