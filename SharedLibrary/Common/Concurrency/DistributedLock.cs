using Common.Driver;
using Microsoft.Extensions.Logging;

namespace Common.Concurrency;

// Redis 싱글 스레드 기반이며, 명령은 원자적으로 처리된다.
//  1. SET lock_key lock_token NX PX 3000
//      ㄴ NX : 키가 없을 때만 설정
//      ㄴ PX : TTL(ms) 설정
//      -> 성공 시 락 획득
//      -> 실패 시 누군가 락 획득한 상태
//  2. 락 해제시 lua 스크립트 사용하기
//      내가 잡은 락만 해제 필요
//      if redis.call("GET", KEYS[1]) == ARGV[1] then
//          return redis.call("DEL", KEY[1])
//      end
//      return 0
////////////////////////////////////////////////////////////
// StackExchange.Redis에서 제공하는 분산 락 API
//  LockTake         : SET NX + TTL 기반 락 획득
//  LockRelease      : 토큰 비교 후 안전한 락 해제
//  LockExtend       : 현재 보유 중인 락의 TTL 연장

public sealed class DistributedLock : IDistributedLock
{
    private const string RELEASE_LOCK_IF_OWNER_SCRIPT =
        "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end";
    
    private readonly ICacheDriver mCacheDriver;
    private readonly TimeSpan? mLockExpiry;

    public DistributedLock(ICacheDriver cacheDriver, long expiryMs)
    {
        mCacheDriver = cacheDriver;
        
        if (expiryMs == 0)
        {
            mLockExpiry = null;
            return;
        }

        mLockExpiry = TimeSpan.FromMilliseconds(expiryMs);
    }

    public async Task<bool> TryAcquireLockAsync(string lockKey, string lockToken)
    {
        return await mCacheDriver
            .StringSetAsync(lockKey, lockToken, mLockExpiry, ICacheDriver.ESetCondition.NotExists);
    }

    public async Task<bool> TryReleaseLockAsync(string lockKey, string lockToken)
    {
        var result = await mCacheDriver
            .ScriptEvaluateAsync(RELEASE_LOCK_IF_OWNER_SCRIPT, [lockKey], [lockToken]);

        return long.TryParse(result, out var redisValue) && redisValue == 1;
    }
}

// 핸들(Handle)
//  어떤 자원을 참조 혹은 제어하는 객체
//  자원 하나를 붙잡고 있다가 나중에 놓아주는 역할을 함
//  즉, 생성(획득)하고 Dispose(해제)하는 개념
/////////////////////////////////////////////////////////////////////////
// 핸들러(Handler)
//  이벤트/요청/메시지가 오면 그걸 처리하는 로직
//  즉, 이벤트/요청/메시지가 들어오면 그 순간 처리하고 끝나는 개념
public class DistributedLockHandle<T> : IAsyncDisposable
{
    private readonly ILogger<T> mLogger;
    private readonly IDistributedLock mDistributedLock;
    
    private readonly string mLockKey;
    private readonly string mLockToken;

    private bool mbAcquired;
    private bool mbDisposed;

    public DistributedLockHandle(
        ILogger<T> logger,
        IDistributedLock distributedLock, 
        string lockKey, 
        string lockToken)
    {
        mLogger = logger;
        mDistributedLock = distributedLock;
        mLockKey = lockKey;
        mLockToken = lockToken;
    }
    
    public async Task<bool> TryAcquireLockAsync()
    {
        mbAcquired = 
            await mDistributedLock.TryAcquireLockAsync(mLockKey, mLockToken);
        return mbAcquired;
    }

    public async ValueTask DisposeAsync()
    {
        if (mbDisposed || !mbAcquired)
            return;
        
        mbDisposed = true;
        
        var bReleased = 
            await mDistributedLock.TryReleaseLockAsync(mLockKey, mLockToken);

        if (!bReleased)
        {
            // TODO 알람/모니터링 연동 고려
            //  다시 생각해보니 결국, 락 해제만 실패했을 뿐, 결제는 성공함
            //  우선 이 부분은 좀 더 고민해보기
            mLogger.LogWarning("Lock release failed: key={key}, token={token}", mLockKey, mLockToken);
        }
    }
}