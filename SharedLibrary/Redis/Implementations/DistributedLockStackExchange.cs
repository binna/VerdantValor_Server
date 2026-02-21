using StackExchange.Redis;

namespace Redis.Implementations;

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

public sealed class DistributedLockStackExchange
{
    private const string RELEASE_LOCK_IF_OWNER_SCRIPT =
        "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end";
    
    private readonly IDatabase mDatabase;
    private readonly TimeSpan mLockExpiry;

    public DistributedLockStackExchange(string host, string port, int db, long expiryMs)
    {
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(port))
            throw new ArgumentException("Host and port must not be null or empty.");
        
        var endpoint = $"{host}:{port}";
        var connection = ConnectionMultiplexer.Connect(endpoint);
        
        mDatabase = connection.GetDatabase(db);
        mLockExpiry = TimeSpan.FromMilliseconds(expiryMs);
    }

    public Task<bool> TryAcquireLockAsync(string lockKey, string lockToken)
    {
        return mDatabase.StringSetAsync(lockKey, lockToken, mLockExpiry, When.NotExists);
    }

    public async Task<bool> TryReleaseLockAsync(string lockKey, string lockToken)
    {
        var result = await mDatabase
            .ScriptEvaluateAsync(RELEASE_LOCK_IF_OWNER_SCRIPT, [lockKey], [lockToken]);

        return (long)result == 1;
    }
}