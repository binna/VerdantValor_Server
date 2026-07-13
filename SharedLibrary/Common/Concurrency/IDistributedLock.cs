namespace Common.Concurrency;

public interface IDistributedLock
{
    Task<bool> TryAcquireGlobalLockAsync();
}