using Common.Types;

namespace Common.Driver;

public interface ICacheDriver
{
    public enum ESetCondition
    {
        None = 0,
        Exists = 1,
        NotExists = 2,
    }
    
    public enum EGetOrder
    {
        Ascending = 0,
        Descending = 1,
    }
    
    // TODO
    //  CommandFlags 필요할까?
    //  1. fire and forget
    //  2. Master와 Slave 구조에서 어떤 걸 쓸지 결정하는거

    Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null, ESetCondition condition = ESetCondition.None, CancellationToken token = default);
    Task<string> StringGetAsync(string key, CancellationToken token = default);
    Task<bool> HashSetAsync(string key, string hashField, string value, ESetCondition condition = ESetCondition.None, CancellationToken token = default);
    Task<bool> SortedSetAddAsync(string key, string member, double score, ESetCondition condition = ESetCondition.None, CancellationToken token = default);
    /* start: 시작 rank(0부터 시작), stop: 끝 rank (-1은 마지막) */
    Task<RankingEntry[]> SortedSetRangeByRankWithScoresAsync(string key, long start = 0, long stop = -1, EGetOrder order = EGetOrder.Ascending, CancellationToken token = default);
    Task<bool> KeyExpireAsync(string key, TimeSpan? expiry = null, CancellationToken token = default);
    Task<long?> SortedSetRankAsync(string key, string member, EGetOrder order = EGetOrder.Ascending, CancellationToken token = default);
    Task<double?> SortedSetScoreAsync(string key, string member, CancellationToken token = default);
    Task<string> ScriptEvaluateAsync(string script, string[] keys, string[] values, CancellationToken token = default);
}