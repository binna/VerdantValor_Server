using Common.Web;
using Protocol.Web.Dtos;
using Shared.Constants;
using Shared.Types;

namespace WebServer.Services;

public class RankingService
{
    private readonly ILogger<RankingService> mLogger;
    private readonly IKeyValueStore mKeyValueStore;

    public RankingService(
        ILogger<RankingService> logger, 
        IKeyValueStore keyValueStore)
    {
        mLogger = logger;
        mKeyValueStore = keyValueStore;
    }

    public async Task<ApiResponse<RankRes>> GetTopRankingAsync(ERanking rankingType, int limit)
    {
        if (limit is < AppConstant.RANKING_MIN or > AppConstant.RANKING_MAX)
            return ApiResponse<RankRes>
                .From(EResponseResult.InvalidInput);

        var redisRankings =
            await mKeyValueStore.GetTopRankingByType(CreateRankingKeyName(rankingType), limit);

        var result = new RankRes();

        for (var i = 0; i < redisRankings.Length; i++)
        {
            var info = redisRankings[i];
            var userInfo = info.Element.Split("/");

            if (!ulong.TryParse(userInfo[0], out var userId))
            {
                mLogger.LogError("Failed to parse userId.\nCheck Redis data. {@userId}", 
                    new { userId = userInfo[0] });
            }
            
            result.Rankings.Add(new RankInfo
            {
                Rank = i + 1,
                Nickname = userInfo[1],
                Score = info.Score
            });
        }

        return ApiResponse<RankRes>
            .From(EResponseResult.Success, result);
    }

    public async Task<ApiResponse<RankRes>> GetMemberRankAsync(ERanking rankingType, string userId, string nickname)
    {
        var member = CreateMemberFieldName(userId, nickname);
        var rankingKey = CreateRankingKeyName(rankingType);
        
        var rank = 
            await mKeyValueStore.GetMemberRank(rankingKey, member);

        var score = 
            await mKeyValueStore.GetMemberScore(rankingKey, member);

        if (rank == null || score == null)
            return ApiResponse<RankRes>
                .From(EResponseResult.SuccessEmptyRanking);
        
        var result = new RankRes();
        result.Rankings.Add(
            new RankInfo
            {
                Rank = (int)rank + 1,
                Nickname = nickname,
                Score = (double)score
            }
        );

        return ApiResponse<RankRes>
            .From(EResponseResult.Success, result);
    }

    public async Task<ApiResponse> AddScore(ERanking rankingType, string userId, string nickname, double score)
    {
        if (score <= 0)
            return ApiResponse
                .From(EResponseResult.ScoreCannotBeNegative);
        
        var member = CreateMemberFieldName(userId, nickname);
        
        await mKeyValueStore.AddSortedSetAsync(CreateRankingKeyName(rankingType), member, score); 
        
        return ApiResponse
            .From(EResponseResult.Success);
    }

    private static string CreateMemberFieldName(string userId, string nickname)
    {
        return $"{userId}/{nickname}";
    }

    private static string CreateRankingKeyName(ERanking rankingType)
    {
        return $"{AppConstant.RANKING_ROOT}:{rankingType}";
    }
}