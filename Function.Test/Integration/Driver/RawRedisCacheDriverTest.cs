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
    public async Task Test_StringGetAsync_Success(string key, string value)
    {
        var rawRedisDriver = new RawRedisCacheDriver("localhost", 6379, 0);

        await rawRedisDriver.StringSetAsync(key, value);
        var response = await rawRedisDriver.StringGetAsync(key);
        
        Assert.Equal(value, response);
    }
}