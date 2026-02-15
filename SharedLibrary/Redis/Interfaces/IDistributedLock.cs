namespace Redis.Interfaces;

public interface IDistributedLock
{
    public Task<bool> TryAcquireLockAsync(string lockKey, string lockToken);
    public Task<bool> TryReleaseLockAsync(string lockKey, string lockToken);
}