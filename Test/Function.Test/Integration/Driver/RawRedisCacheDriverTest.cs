using System.Net.Sockets;
using Common.Driver;
using Xunit.Abstractions;

namespace Function.Test.Integration.Driver;

public class RawRedisCacheDriverTest
{
    private readonly ITestOutputHelper mOutput;
    
    private const string HOST = "localhost";
    private const int PORT = 6379;
    private const int DB = 0;
    
    public RawRedisCacheDriverTest(ITestOutputHelper output)
    {
        mOutput = output;
    }
    
    [Fact]
    public void Test_생성자_Host_Empty_ArgumentException_Throw()
    {
        Assert.Throws<ArgumentException>(() => new RawRedisCacheDriver("", PORT, DB));
    }
    
    [Fact]
    public void Test_생성자_Host_공백으로만이뤄짐_ArgumentException_Throw()
    {
        Assert.Throws<ArgumentException>(() => new RawRedisCacheDriver("        ", PORT, DB));
    }
    
    [Theory]
    [InlineData(-1)]
    [InlineData(-5)]
    [InlineData(-7)]
    [InlineData(-8)]
    public void Test_생성자_Port_음수일때_ArgumentException_Throw(int port)
    {
        Assert.Throws<ArgumentException>(() => new RawRedisCacheDriver(HOST, port, DB));
    }
    
    [Theory]
    [InlineData(-1)]
    [InlineData(-5)]
    [InlineData(-7)]
    [InlineData(-8)]
    public void Test_생성자_Db_음수일때_ArgumentException_Throw(int db)
    {
        Assert.Throws<ArgumentException>(() => new RawRedisCacheDriver(HOST, PORT, db));
    }
    
    [Fact]
    public void Test_생성자_잘못된_레디스정보_연결실패_SocketException_Throw()
    {
        Assert.Throws<SocketException>(() => new RawRedisCacheDriver(HOST, 1004, 0));
    }
    
    [Fact]
    public void Test_생성자_DB인덱스_존재안함_InvalidOperationException_Throw()
    {
        Assert.Throws<InvalidOperationException>(() => new RawRedisCacheDriver(HOST, PORT, 1004));
    }
    
    [Theory]
    [InlineData("test", "shine")]
    [InlineData("test2", "shine2")]
    public async Task Test_StringSetAsync_이후_StringGetAsync_동일값반환(string key, string value)
    {
        var rawRedisDriver = new RawRedisCacheDriver("localhost", 6379, 0);

        await rawRedisDriver.StringSetAsync(key, value, TimeSpan.FromMilliseconds(1000));
        var response = await rawRedisDriver.StringGetAsync(key);
        
        Assert.Equal(value, response);
    }
    
    [Theory]
    [InlineData("test", "shine")]
    [InlineData("test2", "shine2")]
    public async Task Test_StringSetAsync_만료시간_지나면_삭제됨(string key, string value)
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
    public async Task Test_ScriptEvaluateAsync_조건일치시_삭제되고_1반환(string key, string value)
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
    public async Task Test_ScriptEvaluateAsync_만료시간_지나면_0반환(string key, string value)
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
    public async Task Test_SortedSetRangeByRankWithScoresAsync_오름차순정렬()
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
    public async Task Test_SortedSetRangeByRankWithScoresAsync_내림차순정렬()
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