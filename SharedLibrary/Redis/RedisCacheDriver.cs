using System.Buffers;
using Common.Error;
using Common.Driver;
using Common.Types;
using StackExchange.Redis;

namespace Redis;

public class RedisCacheDriver : ICacheDriver, IDisposable
{
    private readonly ConnectionMultiplexer mConnection;
    private readonly IDatabase mDatabase;
    
    public RedisCacheDriver(string host, string port, int db)
    {
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(port))
            throw new ArgumentException(
                string.Format(ErrorMessages.MUST_NOT_BE_NULL_OR_EMPTY, "Host and port"));
        
        var endpoint = $"{host}:{port}";
        
        mConnection = ConnectionMultiplexer.Connect(endpoint);
        mDatabase = mConnection.GetDatabase(db);
    }

    public void Dispose()
    {
        mConnection.Dispose();
    }

    public Task<bool> StringSetAsync(
        string key, 
        string value, 
        TimeSpan? expiry = null, 
        ICacheDriver.ESetCondition condition = ICacheDriver.ESetCondition.None,
        CancellationToken token = default)
    {
        switch (condition)
        {
            case ICacheDriver.ESetCondition.Exists:
                return mDatabase.StringSetAsync(key, value, expiry, When.Exists);
            case ICacheDriver.ESetCondition.NotExists:
                return mDatabase.StringSetAsync(key, value, expiry, When.NotExists);
            case ICacheDriver.ESetCondition.None:
            default:
                return mDatabase.StringSetAsync(key, value, expiry, When.Always);
        }
    }

    public async Task<string> StringGetAsync(string key, CancellationToken token = default)
    {
        var value = await mDatabase.StringGetAsync(key);
        return value.ToString();
    }
    
    public Task<bool> HashSetAsync(
        string key,
        string hashField, 
        string value,
        ICacheDriver.ESetCondition condition = ICacheDriver.ESetCondition.None, 
        CancellationToken token = default)
    {
        switch (condition)
        {
            case ICacheDriver.ESetCondition.Exists:
                return mDatabase.HashSetAsync(key, hashField, value, When.Exists);
            case ICacheDriver.ESetCondition.NotExists:
                return mDatabase.HashSetAsync(key, hashField, value, When.NotExists);
            case ICacheDriver.ESetCondition.None:
            default:
                return mDatabase.HashSetAsync(key, hashField, value, When.Always);
        }
    }
    
    public Task<bool> SortedSetAddAsync(
        string key,
        string member,
        double score,
        ICacheDriver.ESetCondition condition = ICacheDriver.ESetCondition.None,
        CancellationToken token = default)
    {
        switch (condition)
        {
            case ICacheDriver.ESetCondition.Exists:
                return mDatabase.SortedSetAddAsync(key, member, score, When.Exists);
            case ICacheDriver.ESetCondition.NotExists:
                return mDatabase.SortedSetAddAsync(key, member, score, When.NotExists);
            case ICacheDriver.ESetCondition.None:
            default:
                return mDatabase.SortedSetAddAsync(key, member, score, When.Always);
        }
    }

    public async Task<RankingEntry[]> SortedSetRangeByRankWithScoresAsync(
        string key,
        long start = 0,
        long stop = -1,
        ICacheDriver.EGetOrder order = ICacheDriver.EGetOrder.Ascending, 
        CancellationToken token = default)
    {
        SortedSetEntry[] entries;

        switch (order)
        {
            case ICacheDriver.EGetOrder.Descending:
                entries = await mDatabase
                    .SortedSetRangeByRankWithScoresAsync(key, start, stop, Order.Descending);
                break;
            case ICacheDriver.EGetOrder.Ascending:
            default:
                entries = await mDatabase
                    .SortedSetRangeByRankWithScoresAsync(key, start, stop, Order.Ascending);
                break;
        }

        var rankings = ArrayPool<RankingEntry>.Shared.Rent(entries.Length);
        
        for (var i = 0; i < entries.Length; i++)
        {
            rankings[i] = new RankingEntry(entries[i].Element.ToString(), entries[i].Score);
        }

        return rankings;
    }

    public Task<long?> SortedSetRankAsync(
        string key,
        string member,
        ICacheDriver.EGetOrder order = ICacheDriver.EGetOrder.Ascending,
        CancellationToken token = default)
    {
        switch (order)
        {
            case ICacheDriver.EGetOrder.Descending:
                return mDatabase.
                    SortedSetRankAsync(key, member, Order.Descending);
            case ICacheDriver.EGetOrder.Ascending:
            default:
                return mDatabase.
                    SortedSetRankAsync(key, member, Order.Ascending);
        }
    }

    public Task<double?> SortedSetScoreAsync(
        string key,
        string member,
        CancellationToken token = default)
    {
        return mDatabase.SortedSetScoreAsync(key, member);
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
        
        var redisKeys = new RedisKey[keys.Length];
        var redisValues = new RedisValue[values.Length];
        
        for (var i = 0; i <  keys.Length; i++)
        {
            redisKeys[i] = keys[i];
        }
        
        for (var i = 0; i <  values.Length; i++)
        {
            redisValues[i] = values[i];
        }

        var response = await mDatabase.ScriptEvaluateAsync(script, redisKeys, redisValues);
        
        return response.ToString();
    }
}