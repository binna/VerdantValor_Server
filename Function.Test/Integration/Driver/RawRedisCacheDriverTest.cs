using Common.Driver;
using Xunit.Abstractions;

namespace Function.Test.Integration.Driver;

public class RawRedisCacheDriverTest
{
    private readonly ITestOutputHelper mOutput;
    
    public RawRedisCacheDriverTest(ITestOutputHelper output)
    {
        mOutput = output;
    }
    
    [Theory]
    [InlineData("", 6379, 0)]
    [InlineData("    ", 6379, 0)]
    [InlineData("localhost", 0, 0)]
    [InlineData("localhost", -5, 0)]
    [InlineData("localhost", 6379, -1)]
    public void Test_생성자_입력이_유효하지않을때_Throw(string host, int port, int db)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            new RawRedisCacheDriver(host, port, db);
        });
    }
    
    [Theory]
    [InlineData("test", "shine")]
    [InlineData("test2", "shine2")]
    public async Task Test_StringSetAsync_이후_StringGetAsync_동일값반환_Success(string key, string value)
    {
        var rawRedisDriver = new RawRedisCacheDriver("localhost", 6379, 0);

        await rawRedisDriver.StringSetAsync(key, value, TimeSpan.FromMilliseconds(1000));
        var response = await rawRedisDriver.StringGetAsync(key);
        
        Assert.Equal(response, value);
    }
    
    [Theory]
    [InlineData("test", "shine")]
    [InlineData("test2", "shine2")]
    public async Task Test_StringSetAsync_만료시간_지나면_삭제됨_Success(string key, string value)
    {
        var rawRedisDriver = new RawRedisCacheDriver("localhost", 6379, 0);
        
        await rawRedisDriver.StringSetAsync(key, value, TimeSpan.FromMilliseconds(500));

        await Task.Delay(1000);

        var response = await rawRedisDriver.StringGetAsync(key);
        
        Assert.Empty(response);
    }

    [Theory]
    [InlineData("Server1:Area", "gain/1/shine")]
    [InlineData("Server1:Area", "gain/1/binna")]
    public async Task Test_ScriptEvaluateAsync_조건일치시_삭제되고_1반환_Success(string key, string value)
    {
        var rawRedisDriver = new RawRedisCacheDriver("localhost", 6379, 0);

        await rawRedisDriver
            .StringSetAsync(key, value, TimeSpan.FromMilliseconds(1000), ICacheDriver.ESetCondition.NotExists);
        
        var script = await rawRedisDriver.ScriptEvaluateAsync(
            "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end",
            [key], [value]);
        
        Assert.Equal("1", script);
    }
    
    [Theory]
    [InlineData("Server1:Area", "gain/1/shine")]
    [InlineData("Server1:Area", "gain/1/binna")]
    public async Task Test_ScriptEvaluateAsync_만료시간_지나면_0반환_Success(string key, string value)
    {
        var rawRedisDriver = new RawRedisCacheDriver("localhost", 6379, 0);

        await rawRedisDriver
            .StringSetAsync(key, value, TimeSpan.FromMilliseconds(500), ICacheDriver.ESetCondition.NotExists);
        
        await Task.Delay(1000);
        
        var script = await rawRedisDriver.ScriptEvaluateAsync(
            "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end",
            [key], [value]);
        
        mOutput.WriteLine("script result: " + script);
        
        Assert.Equal("0", script);
    }


    [Fact]
    public async Task Test_SortedSetRangeByRankWithScoresAsync_오름차순정렬_Success()
    {
        var rawRedisDriver = new RawRedisCacheDriver("localhost", 6379, 0);
        
        await rawRedisDriver.SortedSetAddAsync("Test:Ranking:All", "shine", 50);
        await rawRedisDriver.SortedSetAddAsync("Test:Ranking:All", "binna", 7);
        await rawRedisDriver.SortedSetAddAsync("Test:Ranking:All", "nice", 1000);

        var result = 
            await rawRedisDriver
                .SortedSetRangeByRankWithScoresAsync("Test:Ranking:All", 0, 50);

        Assert.Equal("binna", result[0].Element);
        Assert.Equal(7, result[0].Score);

        Assert.Equal("shine", result[1].Element);
        Assert.Equal(50, result[1].Score);

        Assert.Equal("nice", result[2].Element);
        Assert.Equal(1000, result[2].Score);
    }
    
    [Fact]
    public async Task Test_SortedSetRangeByRankWithScoresAsync_내림차순정렬_Success()
    {
        var rawRedisDriver = new RawRedisCacheDriver("localhost", 6379, 0);
        
        await rawRedisDriver.SortedSetAddAsync("Test:Ranking:All", "shine", 50);
        await rawRedisDriver.SortedSetAddAsync("Test:Ranking:All", "binna", 7);
        await rawRedisDriver.SortedSetAddAsync("Test:Ranking:All", "nice", 1000);

        var result =
            await rawRedisDriver
                .SortedSetRangeByRankWithScoresAsync("Test:Ranking:All", 0, 50, ICacheDriver.EGetOrder.Descending);
        
        Assert.Equal("nice", result[0].Element);
        Assert.Equal(1000, result[0].Score);

        Assert.Equal("shine", result[1].Element);
        Assert.Equal(50, result[1].Score);
        
        Assert.Equal("binna", result[2].Element);
        Assert.Equal(7, result[2].Score);
    }
}