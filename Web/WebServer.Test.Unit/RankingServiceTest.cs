using Microsoft.Extensions.Logging;
using NSubstitute;
using Redis;
using Shared.Constants;
using Shared.Types;
using StackExchange.Redis;
using WebServer.Services;
using Xunit.Abstractions;

namespace WebServer.Test.Unit;

[Collection("GlobalSetup ResponseStatus")]
public class RankingServiceTest
{
    private readonly ITestOutputHelper mOutput;
    private readonly IRedisClient mRedisClient;
    private readonly RankingService mRankingService;

    public RankingServiceTest(ITestOutputHelper output)
    {
        mOutput = output;
        var logger = Substitute.For<ILogger<RankingService>>();
        mRedisClient = Substitute.For<IRedisClient>();
        mRankingService = Substitute.For<RankingService>(logger, mRedisClient);
    }

    #region TOP 랭킹 조회
    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(49)]
    [InlineData(101)]
    [InlineData(150)]
    [InlineData(200)]
    public async Task Test_GetTopRanking_Limit_길이가_범위_밖일때_Fail(int limit)
    {
        mRedisClient.GetTopRankingByType(
                $"{AppConstant.RANKING_ROOT}:{ERanking.All}", limit)
            .Returns(Task.FromResult(
                new []
                {
                    new SortedSetEntry("1/user1", 1000.0),
                    new SortedSetEntry("2/user2", 900.0),
                    new SortedSetEntry("3/user3", 800.0),
                    new SortedSetEntry("4/user4", 700.0),
                    new SortedSetEntry("5/user5", 600.0),
                }
            ));
        
        var response = await mRankingService.GetTopRankingAsync(ERanking.All, limit);
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.InvalidInput}"); 
        Assert.False(response.IsSuccess);
    }

    [Fact]
    public async Task Test_GetTopRanking_랭킹이_없을때_Success()
    {
        mRedisClient.GetTopRankingByType(
                $"{AppConstant.RANKING_ROOT}:{ERanking.All}", 100)
            .Returns(Task.FromResult(Array.Empty<SortedSetEntry>()));
        
        var response = await mRankingService.GetTopRankingAsync(ERanking.All, 100);
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.Success}"); 
        Assert.True(response.IsSuccess);
    }
    
    [Theory]
    [InlineData(50)]
    [InlineData(70)]
    [InlineData(95)]
    [InlineData(100)]
    public async Task Test_GetTopRanking_Success(int limit)
    {
        mRedisClient.GetTopRankingByType(
            $"{AppConstant.RANKING_ROOT}:{ERanking.All}", limit)
            .Returns(Task.FromResult(
                new []
                {
                    new SortedSetEntry("1/user1", 1000.0),
                    new SortedSetEntry("2/user2", 900.0),
                    new SortedSetEntry("3/user3", 800.0),
                    new SortedSetEntry("4/user4", 700.0),
                    new SortedSetEntry("5/user5", 600.0),
                }
            ));
        
        var response = await mRankingService.GetTopRankingAsync(ERanking.All, limit);
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.Success}"); 
        Assert.True(response.IsSuccess);
    }
    #endregion

    #region 내 랭킹 조회
    [Theory]
    [InlineData("3/user3", "3", "user3", null, null)]
    [InlineData("3/user3", "3", "user3", "3", null)]
    [InlineData("3/user3", "3", "user3", null, "5.5")]
    public async Task Test_GetMemberRank_순위_점수_검색안될때_Success(string member, string userId, string nickname, string? rankText, string? scoreText)
    {
        long? rank = int.TryParse(rankText, out var r) ? r : null;
        double? score = double.TryParse(scoreText, out var s) ? s : null;
        
        mRedisClient.GetMemberRank(
                $"{AppConstant.RANKING_ROOT}:{ERanking.All}", member)
            .Returns(Task.FromResult(rank));
        
        mRedisClient.GetMemberScore(
                $"{AppConstant.RANKING_ROOT}:{ERanking.All}", member)
            .Returns(Task.FromResult(score));
        
        var response = await mRankingService.GetMemberRankAsync(ERanking.All, userId, nickname);
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.SuccessEmptyRanking}"); 
        Assert.True(response.IsSuccess);
    }
    
    [Theory]
    [InlineData("3/user3", "3", "user3", 3, 3000.0)]
    public async Task Test_GetMemberRank_Success(string member, string userId, string nickname, long rank, double score)
    {
        mRedisClient.GetMemberRank(
                $"{AppConstant.RANKING_ROOT}:{ERanking.All}", member)
            .Returns(Task.FromResult<long?>(rank));
        
        mRedisClient.GetMemberScore(
                $"{AppConstant.RANKING_ROOT}:{ERanking.All}", member)
            .Returns(Task.FromResult<double?>(score));
        
        var response = await mRankingService.GetMemberRankAsync(ERanking.All, userId, nickname);
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.Success}"); 
        Assert.True(response.IsSuccess);
    }
    #endregion

    #region 랭킹 추가
    [Theory]
    [InlineData("1", "user1", 0)]
    [InlineData("2", "user2", -1)]
    [InlineData("3", "user3", -110)]
    public async Task Test_AddScore_점수가_음수일때_Fail(string userId, string nickname, double score)
    {
        var response = await mRankingService.AddScore(ERanking.All, userId, nickname, score);
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.ScoreCannotBeNegative}"); 
        Assert.False(response.IsSuccess);
    }

    [Theory]
    [InlineData("1", "user1", 5000)]
    public async Task Test_AddScore_Success(string userId, string nickname, double score)
    {
        var response = await mRankingService.AddScore(ERanking.All, userId, nickname, score);
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.Success}"); 
        Assert.True(response.IsSuccess);
    }
    #endregion
}