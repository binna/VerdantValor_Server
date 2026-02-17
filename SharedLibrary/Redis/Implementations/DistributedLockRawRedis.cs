using System.Net.Sockets;
using System.Text;

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
// SET GainItem shine NX PX 3000
//      OK              : 락 획득 성공
//      (nil)           : 락 획득 실패, 키가 이미 존재
// EVAL "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end" 1 GainItem shine
//      (integer) 1     : 락 삭제 성공 (내가 건 락)
//      (integer) 0     : 키 없음 또는 값 불일치 (내 락 아님)
////////////////////////////////////////////////////////////
// 1 GainItem shine 의미
//      1           : key 개수 (이후 1개는 KEYS 배열로 전달)
//      GainItem    : KEYS[1]
//      shine       : ARGV[1]
// EVAL             : 매번 스크립트 문자열을 함께 전송하여 실행
// SCRIPT LOAD      : 서버에 스크립트 저장 후 해시 반환
// EVALSHA          : 저장된 스크립트를 해시로 실행
////////////////////////////////////////////////////////////
// Redis 싱글 스레드 기반이며, 명령은 원자적으로 처리한다.
// 그래서 처음에는 코드 상에서 별도의 락이 필요 없다고 생각했다.
//
// 하지만 TcpClient로 Redis에 직접 연결해 사용해보니,
// 하나의 TCP 커넥션(NetworkStream)을 여러 비동기 작업이 동시에 사용할 경우
// Read, Write 과정에서 무한 대기가 발생했다.
//
// 이는 Redis의 문제가 아니라,
// 하나의 커넥션에 대해 동시 요청을 허용한 내 코드 설계 문제였다.
//
// 이를 해결하기 위해 한 커넥션에서는 한 번에 하나의 요청만
// 수행되도록 임계영역을 구성하여 동기화 처리했다.
public class DistributedLockRawRedis
{
    private readonly TcpClient mTcpClient;
    private readonly long mLockExpiryMs;
    private readonly NetworkStream mStream;
    
    private readonly SemaphoreSlim mMutex = new(1, 1);

    public DistributedLockRawRedis(string host, int port, int db, long expiryMs)
    {
        mLockExpiryMs = expiryMs;
        mTcpClient = new TcpClient();
        mTcpClient.Connect(host, port);
        mStream = mTcpClient.GetStream();
        
        var requestBytes = Encoding.UTF8.GetBytes("PING\r\n");
        mStream.Write(requestBytes, 0, requestBytes.Length);

        var buffer = new byte[1024];
        var bytesRead = mStream.Read(buffer, 0, buffer.Length);
        var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        if (!response.Contains("PONG"))
            throw new IOException($"Unexpected PING response: {response}");
        
        requestBytes = Encoding.UTF8.GetBytes($"SELECT {db}\r\n");
        mStream.Write(requestBytes, 0, requestBytes.Length);

        buffer = new byte[1024];
        bytesRead = mStream.Read(buffer, 0, buffer.Length);
        response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        if (!response.Contains("OK"))
            throw new InvalidOperationException($"SELECT {db} failed. Response: {response}");
    }
    
    public async Task<bool> TryAcquireLockAsync(string lockKey, string lockToken, CancellationToken cancellationToken)
    {
        var pingCommand = $"SET {lockKey} {lockToken} NX PX {mLockExpiryMs}\r\n";
        var requestBytes = Encoding.UTF8.GetBytes(pingCommand);

        var buffer = new byte[1024];
        int bytesRead;
        
        await mMutex.WaitAsync(cancellationToken);
        try
        {
            await mStream.WriteAsync(requestBytes, cancellationToken);
            bytesRead = await mStream.ReadAsync(buffer, cancellationToken);
        }
        catch (Exception e)
        {
            return false;
        }
        finally
        {
            mMutex.Release();
        }

        var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
        // Console.WriteLine($"[TryAcquireLockAsync] Redis Response: |{response}|");
        // Console.WriteLine($"[TryAcquireLockAsync] Redis Response State: {response.Contains("OK")}");

        return response.Contains("OK");
    }

    public async Task<bool> TryReleaseLockAsync(string lockKey, string lockToken, CancellationToken cancellationToken)
    {
        var pingCommand = $"EVAL \"if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end\" 1 {lockKey} {lockToken}\r\n";
        var requestBytes = Encoding.UTF8.GetBytes(pingCommand);
        
        var buffer = new byte[1024];
        int bytesRead;
        
        await mMutex.WaitAsync(cancellationToken);
        try
        {
            await mStream.WriteAsync(requestBytes, cancellationToken);
            bytesRead = await mStream.ReadAsync(buffer, cancellationToken);
        }
        catch (Exception e)
        {
            return false;
        }
        finally
        {
            mMutex.Release();
        }

        var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
        // Console.WriteLine($"[TryReleaseLockAsync] Redis Response: |{response}|");
        // Console.WriteLine($"[TryReleaseLockAsync] Redis Response State: {response.Contains('1')}");

        return response.Contains('1');
    }
}