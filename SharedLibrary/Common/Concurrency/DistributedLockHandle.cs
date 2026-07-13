using Common.Driver;

namespace Common.Concurrency;

// 핸들(Handle)
//  어떤 자원을 참조 혹은 제어하는 객체
//  자원 하나를 붙잡고 있다가 나중에 놓아주는 역할을 함
//  즉, 생성(획득)하고 Dispose(해제)하는 개념
/////////////////////////////////////////////////////////////////////////
// 핸들러(Handler)
//  이벤트/요청/메시지가 오면 그걸 처리하는 로직
//  즉, 이벤트/요청/메시지가 들어오면 그 순간 처리하고 끝나는 개념
public class DistributedLockHandle : IDistributedLock, IAsyncDisposable
{
    private readonly ICacheDriver mCacheDriver;

    private readonly string mLockKey;
    private readonly string mLockToken;
    private readonly TimeSpan mLockExpiry;
    private readonly int mMaxRetryCount;

    private bool mbAcquired;
    private bool mbDisposed;

    public DistributedLockHandle(
        ICacheDriver cacheDriver, 
        string lockKey, 
        string lockToken,
        long expiryMs, 
        int maxRetryCount = 0)
    {
        if (expiryMs == 0)
            expiryMs = 1000;

        mCacheDriver = cacheDriver;
        mLockExpiry = TimeSpan.FromMilliseconds(expiryMs);
        mLockKey = lockKey;
        mLockToken = lockToken;
        mMaxRetryCount = maxRetryCount;
    }
    
    public async Task<bool> TryAcquireGlobalLockAsync()
    {
        for (var cnt = 0; cnt <= mMaxRetryCount; cnt++)
        {
            if (cnt > 0)
                await Task.Delay(50);

            mbAcquired = await mCacheDriver.TryAcquireGlobalLockAsync(mLockKey, mLockToken, mLockExpiry);

            if (mbAcquired)
                return true;
        }

        return mbAcquired;
    }

    public async ValueTask DisposeAsync()
    {
        if (mbDisposed || !mbAcquired)
            return;
        
        mbDisposed = true;
        
        await mCacheDriver.TryReleaseGlobalLockAsync(mLockKey, mLockToken);
    }
}