using System.Net.Sockets;
using System.Text;
using Common.Error;
using Common.Types;

namespace Common.Driver;

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
// Raw Redis TCP driver
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
////////////////////////////////////////////////////////////
// buffer       : 데이터 저장 공간
// writeCursor  : 데이터가 끝난 위치
// readCursor   : 내가 읽은 위치

public class RawRedisCacheDriver : ICacheDriver, IDisposable
{
    private class RespResult
    {
        public int ArrayLength { get; set; }
        public List<string> Data { get; } = [];
    }
    
    private readonly TcpClient mTcpClient;
    private readonly NetworkStream mStream;
    private readonly SemaphoreSlim mMutex = new(1, 1);

    private const int BUFFER_SIZE = 1024;
    
    private byte[] mBuffer = new byte[BUFFER_SIZE];
    private int mReadCursor;
    private int mWriteCursor;
    private string mPartialResponse = "";

    public RawRedisCacheDriver(string host, int port, int db)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException(
                string.Format(ErrorMessages.MUST_NOT_BE_NULL_OR_EMPTY, "Host"));
        
        if (port <= 0)
            throw new ArgumentException(
                string.Format(ErrorMessages.MUST_BE_GREATER_THAN_ZERO, "Port"));
        
        if (db < 0)
            throw new ArgumentException(
                string.Format(ErrorMessages.MUST_BE_NON_NEGATIVE, "DB"));
        
        mTcpClient = new TcpClient();
        mTcpClient.Connect(host, port);
        mStream = mTcpClient.GetStream();
        
        mStream.Write("PING\r\n"u8);

        var response = ReadResponse().Data[0];
        if (!response.StartsWith("PONG", StringComparison.Ordinal))
            throw new InvalidOperationException(
                string.Format(ErrorMessages.RESP_MISMATCH, "PONG", response));
        
        mStream.Write(Encoding.UTF8.GetBytes($"SELECT {db}\r\n"));
        
        response = ReadResponse().Data[0];
        if (!response.StartsWith("OK", StringComparison.Ordinal))
            throw new InvalidOperationException(
                string.Format(ErrorMessages.RESP_MISMATCH, "OK", response));
    }
    
    public void Dispose()
    {
        mStream.Dispose();
        mTcpClient.Dispose();
        mMutex.Dispose();
    }

    public async Task<bool> StringSetAsync(
        string key, 
        string value, 
        TimeSpan? expiry = null, 
        ICacheDriver.ESetCondition condition = ICacheDriver.ESetCondition.None, 
        CancellationToken token = default)
    { 
        var command = new StringBuilder(128);
        command.Append($"SET {key} {value} ");

        switch (condition)
        {
            case ICacheDriver.ESetCondition.Exists:
                command.Append("XX ");
                break;
            case ICacheDriver.ESetCondition.NotExists:
                command.Append("NX ");
                break;
            case ICacheDriver.ESetCondition.None:
            default:
                break;
        }

        if (expiry.HasValue)
            command.Append($"PX {(long)expiry.Value.TotalMilliseconds} ");

        command.Append("\r\n");

        await mMutex.WaitAsync(token);
        try
        {
            await mStream.WriteAsync(
                Encoding.UTF8.GetBytes(command.ToString()), token);

            var response = (await ReadResponseAsync(token)).Data[0];
            return response.StartsWith("OK", StringComparison.Ordinal);
        }
        finally
        {
            mMutex.Release();
        }
    }

    public async Task<string> StringGetAsync(string key, CancellationToken token = default)
    {
        await mMutex.WaitAsync(token);
        try
        {
            await mStream.WriteAsync(Encoding.UTF8.GetBytes($"GET {key}\r\n"), token);
            
            var response = (await ReadResponseAsync(token)).Data[0];
            return response;
        }
        finally
        {
            mMutex.Release();
        }
    }

    public async Task<bool> HashSetAsync(
        string key, 
        string hashField, 
        string value, 
        ICacheDriver.ESetCondition condition = ICacheDriver.ESetCondition.None,
        CancellationToken token = default)
    {
        // HSET key field value
        var command = new StringBuilder(128);
        command.Append($"HSET {key} {hashField} {value} ");
        
        // HSET에서는 NX, XX 지원 안함
        // pass-through

        command.Append("\r\n");

        await mMutex.WaitAsync(token);
        try
        {
            await mStream.WriteAsync(Encoding.UTF8.GetBytes(command.ToString()), token);
            
            var response = (await ReadResponseAsync(token)).Data[0];
            return response == "1";
        }
        finally
        {
            mMutex.Release();
        }
    }

    public async Task<bool> SortedSetAddAsync(
        string key, 
        string member, 
        double score, 
        ICacheDriver.ESetCondition condition = ICacheDriver.ESetCondition.None,
        CancellationToken token = default)
    {
        // ZADD key score member
        var command = new StringBuilder(128);
        command.Append($"ZADD {key} ");
        
        switch (condition)
        {
            case ICacheDriver.ESetCondition.Exists:
                command.Append("XX ");
                break;
            case ICacheDriver.ESetCondition.NotExists:
                command.Append("NX ");
                break;
            case ICacheDriver.ESetCondition.None:
            default:
                break;
        }
        
        command.Append($"{score} {member}\r\n");

        await mMutex.WaitAsync(token);
        try
        {
            await mStream.WriteAsync(Encoding.UTF8.GetBytes(command.ToString()), token);

            var response = (await ReadResponseAsync(token)).Data[0];
            return response == "1";
        }
        finally
        {
            mMutex.Release();
        }
    }

    public async Task<RankingEntry[]> SortedSetRangeByRankWithScoresAsync(
        string key, 
        long start = 0, 
        long stop = -1,
        ICacheDriver.EGetOrder order = ICacheDriver.EGetOrder.Ascending, 
        CancellationToken token = default)
    {
        // ZRANGE key start stop WITHSCORES         오름차순
        // ZREVRANGE key start stop WITHSCORES      내림차순
        // ZRANGE key start stop REV WITHSCORES     내림차순(Redis 6 이후)
        var command = new StringBuilder(128);
        
        switch (order)
        {
            case ICacheDriver.EGetOrder.Descending:
                command.Append($"ZREVRANGE {key} {start} {stop} WITHSCORES\r\n");
                break;
            case ICacheDriver.EGetOrder.Ascending:
            default:
                command.Append($"ZRANGE {key} {start} {stop} WITHSCORES\r\n");
                break;
        }
        
        await mMutex.WaitAsync(token);
        try
        {
            await mStream.WriteAsync(Encoding.UTF8.GetBytes(command.ToString()), token);
            
            var response = await ReadResponseAsync(token);
            var result = new RankingEntry[response.ArrayLength / 2];

            for (var i = 0; i < response.ArrayLength; i += 2)
            {
                if (double.TryParse(response.Data[i + 1], out var score))
                    result[i / 2] = new RankingEntry { Element = response.Data[i], Score = score };
            }

            return result;
        }
        finally
        {
            mMutex.Release();
        }
    }

    public async Task<long?> SortedSetRankAsync(
        string key, 
        string member, ICacheDriver.EGetOrder order = ICacheDriver.EGetOrder.Ascending,
        CancellationToken token = default)
    {
        // ZRANK key member     오름차순
        // ZREVRANK key member  내림차순
        var command = new StringBuilder(128);
        
        switch (order)
        {
            case ICacheDriver.EGetOrder.Descending:
                command.Append($"ZREVRANK {key} {member}\r\n");
                break;
            case ICacheDriver.EGetOrder.Ascending:
            default:
                command.Append($"ZRANK {key} {member}\r\n");
                break;
        }

        await mMutex.WaitAsync(token);
        try
        {
            await mStream.WriteAsync(Encoding.UTF8.GetBytes(command.ToString()), token);
            
            var response = (await ReadResponseAsync(token)).Data[0];

            if (response == "")
                return null;
            
            if (!long.TryParse(response, out var rank))
                throw new FormatException(
                    string.Format(ErrorMessages.RESP_PARSE_ERROR, response));
            
            return rank;
        }
        finally
        {
            mMutex.Release();
        }
    }

    public async Task<double?> SortedSetScoreAsync(string key, string member, CancellationToken token = default)
    {
        // ZSCORE key member
        var command = new StringBuilder(128);
        command.Append($"ZSCORE {key} {member} ");
        command.Append("\r\n");
        
        await mMutex.WaitAsync(token);
        try
        {
            await mStream.WriteAsync(Encoding.UTF8.GetBytes(command.ToString()), token);
            
            var response = (await ReadResponseAsync(token)).Data[0];

            if (response == "")
                return null;
            
            if (!double.TryParse(response, out var score))
                throw new FormatException(
                    string.Format(ErrorMessages.RESP_PARSE_ERROR, response));
            
            return score;
        }
        finally
        {
            mMutex.Release();
        }
    }

    public async Task<string> ScriptEvaluateAsync(
        string script, 
        string[] keys, 
        string[] values, 
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(script))
            throw new ArgumentException(
                string.Format(ErrorMessages.MUST_NOT_BE_EMPTY, "Script"));
        
        var command = new StringBuilder(128);
        command.Append($"EVAL \"{script}\" {keys.Length}");

        if (keys.Length != 0)
        {
            var scriptKeys = string.Join(" ", keys);
            command.Append($" {scriptKeys}");
        }

        if (values.Length != 0)
        {
            var scriptValues = string.Join(" ", values);
            command.Append($" {scriptValues}");
        }

        command.Append("\r\n");
        
        await mMutex.WaitAsync(token);
        try
        {
            await mStream.WriteAsync(
                Encoding.UTF8.GetBytes(command.ToString()), token);
            return (await ReadResponseAsync(token)).Data[0];
        }
        finally
        {
            mMutex.Release();
        }
    }

    private async Task<RespResult> ReadResponseAsync(CancellationToken token)
    {
        RespResult respResult = new();
        
        var isFirstLine = true;
        var arrayLength = 0;
        
        while (!token.IsCancellationRequested)
        {
            for (var i = mReadCursor; i + 1 < mWriteCursor; i++)
            {
                if (mBuffer[i] != '\r' || mBuffer[i + 1] != '\n')
                    continue;
                
                var count = i - mReadCursor;
                var response = mPartialResponse + Encoding.UTF8.GetString(mBuffer, mReadCursor, count);
                
                mPartialResponse = "";
                mReadCursor = i + 2;

                if (mReadCursor >= mWriteCursor)
                {
                    mReadCursor = 0;
                    mWriteCursor = 0;
                }

                if (isFirstLine)
                {
                    isFirstLine = false;
                    NeedsMoreData(ref response, ref respResult);
                    arrayLength = respResult.ArrayLength;
                    
                    if (arrayLength <= 0)
                        respResult.Data.Add(response);
                }
                else if (response.Length == 0 || response[0] != '$')
                {
                    respResult.Data.Add(response);
                    arrayLength--;
                }

                if (arrayLength <= 0) 
                    return respResult;
                
                i++;
            }
            
            if (mWriteCursor == mBuffer.Length)
            {
                var remain = mWriteCursor - mReadCursor;
                
                if (mBuffer[mWriteCursor - 1] == 0x0D)
                {
                    if (remain > 1)
                        mPartialResponse += Encoding.UTF8.GetString(mBuffer, mReadCursor, remain - 1);
                    
                    mBuffer[0] = (byte)'\r';
                    mWriteCursor = 1;
                }
                else
                {
                    mWriteCursor = 0;
                    
                    if (remain > 0)
                        mPartialResponse += Encoding.UTF8.GetString(mBuffer, mReadCursor, remain);
                }
                
                mReadCursor = 0;
            }

            var bytesRead = await mStream.ReadAsync(
                mBuffer.AsMemory(mWriteCursor, mBuffer.Length - mWriteCursor), token);
            
            if (bytesRead <= 0)
                throw new IOException(ErrorMessages.REDIS_DISCONNECTED);

            mWriteCursor += bytesRead;
        }
        
        throw new OperationCanceledException(
            string.Format(ErrorMessages.OPERATION_CANCELED, nameof(ReadResponseAsync)));
    }

    private RespResult ReadResponse()
    {
        RespResult respResult = new();
        
        var isFirstLine = true;
        var arrayLength = 0;
        
        while (true)
        {
            for (var i = mReadCursor; i + 1 < mWriteCursor; i++)
            {
                if (mBuffer[i] != '\r' || mBuffer[i + 1] != '\n')
                    continue;
                
                var count = i - mReadCursor;
                var response = mPartialResponse + Encoding.UTF8.GetString(mBuffer, mReadCursor, count);
                
                mPartialResponse = "";
                mReadCursor = i + 2;

                if (mReadCursor >= mWriteCursor)
                {
                    mReadCursor = 0;
                    mWriteCursor = 0;
                }

                if (isFirstLine)
                {
                    isFirstLine = false;
                    NeedsMoreData(ref response, ref respResult);
                    arrayLength = respResult.ArrayLength;
                    
                    if (arrayLength <= 0)
                        respResult.Data.Add(response);
                }
                else if (response.Length == 0 || response[0] != '$')
                {
                    respResult.Data.Add(response);
                    arrayLength--;
                }

                if (arrayLength <= 0) 
                    return respResult;
                
                i++;
            }

            if (mWriteCursor == mBuffer.Length)
            {
                var remain = mWriteCursor - mReadCursor;

                if (mBuffer[mWriteCursor - 1] == 0x0D)
                {
                    if (remain > 1)
                        mPartialResponse += Encoding.UTF8.GetString(mBuffer, mReadCursor, remain - 1);
                    
                    mBuffer[0] = (byte)'\r';
                    mWriteCursor = 1;
                }
                else
                {
                    mWriteCursor = 0;

                    if (remain > 0)
                        mPartialResponse += Encoding.UTF8.GetString(mBuffer, mReadCursor, remain);
                }

                mReadCursor = 0;
            }

            var bytesRead = mStream.Read(mBuffer, mWriteCursor, mBuffer.Length - mWriteCursor);

            if (bytesRead <= 0)
                throw new IOException(ErrorMessages.REDIS_DISCONNECTED);

            mWriteCursor += bytesRead;
        }
    }
    
   private bool NeedsMoreData(ref string response, ref RespResult respResult)
    {
        if (string.IsNullOrEmpty(response))
            throw new InvalidDataException(
                string.Format(ErrorMessages.MUST_NOT_BE_NULL_OR_EMPTY, "RESP response"));
        
        switch (response[0])
        {
            case '-': // Error
                throw new InvalidOperationException(
                    string.Format(ErrorMessages.RESP_ERROR, response));
            case '+': // Simple String
            case ':': // Integer
                response = response[1..];
                return false;
            case '$': // Bulk String
            {
                if (!int.TryParse(response[1..], out var length))
                    throw new InvalidDataException(
                        string.Format(ErrorMessages.RESP_INVALID_BULK_LENGTH, response));

                if (length < -1)
                    throw new InvalidDataException(
                        string.Format(ErrorMessages.RESP_INVALID_BULK_LENGTH, length));
                
                // $0: empty string
                // $-1: null
                response = "";
                
                if (length == -1)
                    return false;
                
                respResult.ArrayLength = 1;
                return true;
            }
            case '*': // Array
            {
                if (!int.TryParse(response[1..], out var length))
                    throw new InvalidDataException(
                        string.Format(ErrorMessages.RESP_INVALID_ARRAY_LENGTH, response));
                
                response = "";
               
                respResult.ArrayLength = length;
                return true;
            }
            default:
                throw new InvalidDataException(
                    string.Format(ErrorMessages.RESP_UNSUPPORTED_TYPE, response));
        }
    }
}


