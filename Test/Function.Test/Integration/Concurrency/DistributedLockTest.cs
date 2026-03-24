using Common.Concurrency;
using Common.Driver;
using Redis;
using Xunit.Abstractions;

namespace Function.Test.Integration.Concurrency;

public class DistributedLockTest
{
    private readonly ITestOutputHelper mOutput;
    private readonly ICacheDriver mCacheDriver;
    private readonly DistributedLock mDistributedLock;
    
    public DistributedLockTest(ITestOutputHelper output)
    {
        mOutput = output;
        // mCacheDriver = new RawRedisCacheDriver("localhost", 6379, 10);
        mCacheDriver = new RedisCacheDriver("localhost", "6379", 10);
        mDistributedLock = new DistributedLock(mCacheDriver, 1000);
    }

    [Theory]
    [InlineData("GainItem:1", "RandomToken", 5)]
    public async Task Test_TryAcquireLockAsync_동시호출시_하나만_획득(string lockKey, string lockToken, int repeatNum)
    {
        var acquiredCount = 0;
        List<Task> acquireTasks = [];
        
        for (var i = 0; i < repeatNum; i++)
        {
            acquireTasks.Add(Task.Run(async () =>
            {
                var bAcquired = 
                    await mDistributedLock.TryAcquireLockAsync(lockKey, lockToken);

                if (bAcquired)
                    Interlocked.Increment(ref acquiredCount);
            }));
        }

        await Task.WhenAll(acquireTasks);

        Assert.Equal(1, acquiredCount);
    }
    
    [Theory]
    [InlineData("GainItem:2", "RandomToken")]
    public async Task Test_TryReleaseLockAsync_락획득후_정상토큰으로_해제(string lockKey, string lockToken)
    {
        var bAcquired = 
            await mDistributedLock.TryAcquireLockAsync(lockKey, lockToken);
        
        Assert.True(bAcquired);
    
        var bReleased = 
            await mDistributedLock.TryReleaseLockAsync(lockKey, lockToken);
        
        Assert.True(bReleased);
    }
    
    [Theory]
    [InlineData("GainItem:3", "RandomToken")]
    public async Task Test_TryReleaseLockAsync_락획득없이_해제시_Fail(string lockKey, string lockToken)
    {
        var bReleased = 
            await mDistributedLock.TryReleaseLockAsync(lockKey, lockToken);
    
        Assert.False(bReleased);
    }
    
    [Theory]
    [InlineData("GainItem:4", "RandomToken")]
    public async Task Test_만료시간_지나면_Fail(string lockKey, string lockToken)
    {
        var bAcquired = 
            await mDistributedLock.TryAcquireLockAsync(lockKey, lockToken);
        
        Assert.True(bAcquired);
        
        await Task.Delay(1500);
        
        var bReleased = 
            await mDistributedLock.TryReleaseLockAsync(lockKey, lockToken);
        
        Assert.False(bReleased);
    }
}