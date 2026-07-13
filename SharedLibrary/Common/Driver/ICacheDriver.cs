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
    Task<double?> SortedSetScoreAsync(string key, string membe트r, CancellationToken token = default);
    Task<string> ScriptEvaluateAsync(string script, string[] keys, string[] values, CancellationToken token = default);
    
    /*
     * 분산 락(DistributedLock)
     *  여러 서버 인스턴스가 동시에 접근할 수 있는 자원에 대해
     *  한 번에 하나의 요청만 처리될 수 있도록 상호배제를 보장한다.
     *  이때, TTL은 반드시 필요!!
     *  TTL이 없으면 락을 잡은 서버가 죽거나 문제가 생겼을 때,
     *  그 락은 영원히 풀리지 않아 시스템 전체가 멈출 수 있기 때문이다.
     *
     *  TryAcquireGlobalLockAsync : SET NX + TTL 기반으로 락 획득
     *  TryExtendGlobalLockAsync  : 현재 보유 중인 락의 TTL 연장
     *  TryReleaseGlobalLockAsync : 토큰 비교 후, 내가 잡은 락만 안전하게 해제
     */
    Task<bool> TryAcquireGlobalLockAsync(string lockKey, string lockToken, TimeSpan lockExpiry, CancellationToken token = default);
    Task<bool> TryExtendGlobalLockAsync(string lockKey, string lockToken, TimeSpan lockExpiry, CancellationToken token = default);
    Task<bool> TryReleaseGlobalLockAsync(string lockKey, string lockToken, CancellationToken token = default);
}