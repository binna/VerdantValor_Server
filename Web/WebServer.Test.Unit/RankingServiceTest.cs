using Common.Types;
using Common.Web;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Constants;
using Shared.Types;
using WebServer.Services;
using Xunit.Abstractions;

namespace WebServer.Test.Unit;

[Collection("GlobalSetup ResponseStatus")]
public class RankingServiceTest
{
    private readonly ITestOutputHelper mOutput;
    private readonly IKeyValueStore mKeyValueStore;
    private readonly RankingService mRankingService;

    public RankingServiceTest(ITestOutputHelper output)
    {
        mOutput = output;
        mKeyValueStore = Substitute.For<IKeyValueStore>();
        mRankingService = Substitute.For<RankingService>(
            Substitute.For<ILogger<RankingService>>(),
            mKeyValueStore);
    }

    #region Top 랭킹 조회
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
        mKeyValueStore.GetTopRankingByType(
                $"{AppConstant.RANKING_ROOT}:{ERanking.All}", limit)
            .Returns(Task.FromResult(
                new []
                {
                    new RankingEntry("1/user1", 1000.0),
                    new RankingEntry("2/user2", 900.0),
                    new RankingEntry("3/user3", 800.0),
                    new RankingEntry("4/user4", 700.0),
                    new RankingEntry("5/user5", 600.0),
                }
            ));
        
        var response = await mRankingService
            .GetTopRankingAsync(ERanking.All, limit);
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.InvalidInput}"); 
        Assert.False(response.IsSuccess);
    }

    [Fact]
    public async Task Test_GetTopRanking_랭킹이_없을때_Success()
    {
        mKeyValueStore.GetTopRankingByType(
                $"{AppConstant.RANKING_ROOT}:{ERanking.All}", 100)
            .Returns(Task.FromResult(Array.Empty<RankingEntry>()));
        
        var response = await mRankingService
            .GetTopRankingAsync(ERanking.All, 100);
        
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
        mKeyValueStore.GetTopRankingByType(
            $"{AppConstant.RANKING_ROOT}:{ERanking.All}", limit)
            .Returns(Task.FromResult(
                new []
                {
                    new RankingEntry("1/user1", 1000.0),
                    new RankingEntry("2/user2", 900.0),
                    new RankingEntry("3/user3", 800.0),
                    new RankingEntry("4/user4", 700.0),
                    new RankingEntry("5/user5", 600.0),
                }
            ));
        
        var response = await mRankingService
            .GetTopRankingAsync(ERanking.All, limit);
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.Success}"); 
        Assert.True(response.IsSuccess);
    }
    #endregion

    #region 내 랭킹 조회
    [Theory]
    [InlineData("3", "user3", null, 3.0D)]
    [InlineData("3", "user3", 5L, null)]
    [InlineData("3", "user3", null, null)]
    public async Task Test_GetMemberRank_순위_점수_검색안될때_Success(string userId, string nickname, long? rank, double? score)
    {
        mKeyValueStore
            .GetMemberRank(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(rank));
        
        mKeyValueStore
            .GetMemberScore(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(score));
            
        var response = await mRankingService
            .GetMemberRankAsync(ERanking.All, userId, nickname);
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.SuccessEmptyRanking}"); 
        Assert.True(response.IsSuccess);
    }
    
    [Theory]
    [InlineData("3", "user3", 3L, 3000.0D)]
    public async Task Test_GetMemberRank_Success(string userId, string nickname, long? rank, double? score)
    {
        mKeyValueStore
            .GetMemberRank(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(rank));
        
        mKeyValueStore
            .GetMemberScore(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(score));
        
        var response = await mRankingService
            .GetMemberRankAsync(ERanking.All, userId, nickname);
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.Success}"); 
        Assert.True(response.IsSuccess);
    }
    #endregion

    #region 랭킹 추가
    [Theory]
    [InlineData("1", "user1", 0.0D)]
    [InlineData("2", "user2", -1.0D)]
    [InlineData("3", "user3", -110.0D)]
    public async Task Test_AddScore_점수가_0또는음수일때_Fail(string userId, string nickname, double score)
    {
        var response = await mRankingService
            .AddScore(ERanking.All, userId, nickname, score);
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.ScoreCannotBeNegative}"); 
        Assert.False(response.IsSuccess);
    }

    [Theory]
    [InlineData("1", "user1", 5000.0D)]
    public async Task Test_AddScore_Success(string userId, string nickname, double score)
    {
        var response = await mRankingService
            .AddScore(ERanking.All, userId, nickname, score);
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.Success}"); 
        Assert.True(response.IsSuccess);
    }
    #endregion
}