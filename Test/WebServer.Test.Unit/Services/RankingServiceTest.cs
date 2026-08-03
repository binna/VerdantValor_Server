using Common.KeyValueStore;
using Common.Types;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Constants;
using Shared.Types;
using WebServer.Services;
using Xunit.Abstractions;

namespace WebServer.Test.Unit.Services;

[Collection("GlobalSetup ResponseStatus")]
public class RankingServiceTest
{
    private readonly ITestOutputHelper mOutput;
    private readonly IWebKeyValueStore mKeyValueStore;
    private readonly RankingService mRankingService;

    public RankingServiceTest(ITestOutputHelper output)
    {
        mOutput = output;
        mKeyValueStore = Substitute.For<IWebKeyValueStore>();
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
        var rankings = new[]
        {
            new RankingEntry("1/user1", 1000.0),
            new RankingEntry("2/user2", 900.0),
            new RankingEntry("3/user3", 800.0),
            new RankingEntry("4/user4", 700.0),
            new RankingEntry("5/user5", 600.0),
        };
        
        mKeyValueStore.GetTopRankingByType(
                $"{AppConstant.RANKING_ROOT}:{ERanking.All}", limit)
            .Returns(Task.FromResult(rankings));
        
        var result = await mRankingService.GetTopRankingAsync(ERanking.All, limit);
        
        Assert.Equal($"{EResponseResult.InvalidInput}", $"{result.Item1}");
    }

    [Fact]
    public async Task Test_GetTopRanking_랭킹이_없을때_Success()
    {
        mKeyValueStore.GetTopRankingByType(
                $"{AppConstant.RANKING_ROOT}:{ERanking.All}", 100)
            .Returns(Task.FromResult(Array.Empty<RankingEntry>()));
        
        var result = await mRankingService.GetTopRankingAsync(ERanking.All, 100);
        
        Assert.Equal($"{EResponseResult.Success}", $"{result.Item1}");
        Assert.Empty(result.Item2.Rankings);
    }
    
    [Fact]
    public async Task Test_GetTopRanking_파싱_실패한_항목은_결과에서_제외됨()
    {
        var rankings = new[]
        {
            new RankingEntry("1/user1", 1000.0),
            new RankingEntry("2/user2", 900.0),
            new RankingEntry("3user3", 800.0),
            new RankingEntry("4/user4", 700.0),
            new RankingEntry("5/user5", 600.0),
        };
        
        mKeyValueStore.GetTopRankingByType(
                $"{AppConstant.RANKING_ROOT}:{ERanking.All}", 100)
            .Returns(Task.FromResult(rankings));
        
        var result = await mRankingService.GetTopRankingAsync(ERanking.All, 100);
        
        Assert.Equal($"{EResponseResult.Success}", $"{result.Item1}"); 
        Assert.Equal(rankings.Length - 1, result.Item2.Rankings.Count);
        Assert.Equal("user1", result.Item2.Rankings[0].Nickname);
        Assert.Equal("user2", result.Item2.Rankings[1].Nickname);
        Assert.Equal("user4", result.Item2.Rankings[2].Nickname);
        Assert.Equal("user5", result.Item2.Rankings[3].Nickname);
    }
    
    [Fact]
    public async Task Test_GetTopRanking_Success()
    {
        var rankings = new[]
        {
            new RankingEntry("1/user1", 1000.0),
            new RankingEntry("2/user2", 900.0),
            new RankingEntry("3/user3", 800.0),
            new RankingEntry("4/user4", 700.0),
            new RankingEntry("5/user5", 600.0),
        };
        
        mKeyValueStore.GetTopRankingByType(
            $"{AppConstant.RANKING_ROOT}:{ERanking.All}", 100)
            .Returns(Task.FromResult(rankings));
        
        var result = await mRankingService.GetTopRankingAsync(ERanking.All, 100);
        
        Assert.Equal($"{EResponseResult.Success}", $"{result.Item1}"); 
        Assert.Equal(rankings.Length, result.Item2.Rankings.Count);
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
            
        var result = await mRankingService.GetMemberRankAsync(ERanking.All, userId, nickname);
        
        Assert.Equal($"{EResponseResult.SuccessEmptyRanking}", $"{result.Item1}");
        Assert.Empty(result.Item2.Rankings);
    }
    
    [Theory]
    [InlineData("3", "user3", 3L, 3000.0D)]
    public async Task Test_GetMemberRank_Success(string userId, string nickname, long? rank, double? score)
    {
        mKeyValueStore
            .GetMemberRank(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(rank - 1));
        
        mKeyValueStore
            .GetMemberScore(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(score));
        
        var result = await mRankingService.GetMemberRankAsync(ERanking.All, userId, nickname);
        
        Assert.Equal($"{EResponseResult.Success}", $"{result.Item1}"); 
        Assert.Single(result.Item2.Rankings); 
        Assert.Equal(nickname, result.Item2.Rankings[0].Nickname); 
        Assert.Equal(rank, result.Item2.Rankings[0].Rank); 
        Assert.Equal(score, result.Item2.Rankings[0].Score); 
    }
    #endregion
    
    #region 랭킹 추가
    [Theory]
    [InlineData("1", "user1", 0.0D)]
    [InlineData("2", "user2", -1.0D)]
    [InlineData("3", "user3", -110.0D)]
    public async Task Test_AddScore_점수가_0또는음수일때_Fail(string userId, string nickname, double score)
    {
        var code = await mRankingService.AddScore(ERanking.All, userId, nickname, score);
        
        Assert.Equal($"{EResponseResult.ScoreCannotBeNegative}", $"{code}"); 
    }
    
    [Theory]
    [InlineData("1", "user1", 5000.0D)]
    public async Task Test_AddScore_Success(string userId, string nickname, double score)
    {
        var code = await mRankingService.AddScore(ERanking.All, userId, nickname, score);
        
        Assert.Equal( $"{EResponseResult.Success}", $"{code}"); 
    }
    #endregion
}