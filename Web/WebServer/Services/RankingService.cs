using Common.Web;
using Protocol.Web.Dtos;
using Redis;
using Shared.Constants;
using Shared.Types;

namespace WebServer.Services;

public class RankingService
{
    private readonly ILogger<RankingService> mLogger;
    private readonly IRedisClient mRedisClient;

    public RankingService(
        ILogger<RankingService> logger, 
        IRedisClient redisClient)
    {
        mLogger = logger;
        mRedisClient = redisClient;
    }

    public async Task<ApiResponse<RankRes>> GetTopRankingAsync(
        ERanking rankingType, int limit, 
        ELanguage language = ELanguage.En)
    {
        if (limit is < AppConstant.RANKING_MIN or > AppConstant.RANKING_MAX)
            return ApiResponse<RankRes>
                .From(EResponseResult.InvalidInput, language);

        var redisRankings =
            await mRedisClient.GetTopRankingByType(CreateRankingKeyName(rankingType), limit);

        var result = new RankRes();

        for (var i = 0; i < redisRankings.Length; i++)
        {
            var info = redisRankings[i];
            var userInfo = info.Element.ToString().Split("/");

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
            .From(EResponseResult.Success, language, result);
    }

    public async Task<ApiResponse<RankRes>> GetMemberRankAsync(
        ERanking rankingType, 
        string userId, string nickname, 
        ELanguage language = ELanguage.En)
    {
        var member = CreateMemberFieldName(userId, nickname);
        var rankingKey = CreateRankingKeyName(rankingType);
        
        var rank = 
            await mRedisClient.GetMemberRank(rankingKey, member);

        var score = 
            await mRedisClient.GetMemberScore(rankingKey, member);

        if (rank == null || score == null)
            return ApiResponse<RankRes>
                .From(EResponseResult.SuccessEmptyRanking, language);
        
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
            .From(EResponseResult.Success, language, result);
    }

    public async Task<ApiResponse> AddScore(
        ERanking rankingType, 
        string userId, string nickname, double score, 
        ELanguage language = ELanguage.En)
    {
        if (score <= 0)
            return ApiResponse
                .From(EResponseResult.ScoreCannotBeNegative, language);
        
        var member = CreateMemberFieldName(userId, nickname);
        
        await mRedisClient.AddSortedSetAsync(CreateRankingKeyName(rankingType), member, score); 
        
        return ApiResponse
            .From(EResponseResult.Success, language);
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