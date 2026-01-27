using Common.Base;
using VerdantValorShared.Common.Web;
using VerdantValorShared.DTOs.Web;
using Redis;

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
        AppEnum.ERankingType rankingType, int limit, 
        AppEnum.ELanguage language = AppEnum.ELanguage.En)
    {
        if (limit is < AppConstant.RANKING_MIN or > AppConstant.RANKING_MAX)
            return ApiResponse<RankRes>
                .From(AppEnum.EResponseStatus.InvalidInput, language);

        var redisRankings =
            await mRedisClient.GetTopRankingByType(CreateRankingKeyName(rankingType), limit);

        List<RankInfo> rankingList = new(limit);
        
        var result = new RankRes { Rankings = rankingList };

        for (var i = 0; i < redisRankings.Length; i++)
        {
            var info = redisRankings[i];
            var userInfo = info.Element.ToString().Split("/");

            if (!ulong.TryParse(userInfo[0], out var userId))
            {
                mLogger.LogError("Failed to parse userId.\nCheck Redis data. {@userId}", 
                    new { userId = userInfo[0] });
            }
            
            rankingList.Add(new RankInfo
            {
                Rank = i + 1,
                Nickname = userInfo[1],
                Score = info.Score
            });
        }

        return ApiResponse<RankRes>
            .From(AppEnum.EResponseStatus.Success, language, result);

    }

    public async Task<ApiResponse<RankRes>> GetMemberRankAsync(
        AppEnum.ERankingType rankingType, 
        string userId, string nickname, 
        AppEnum.ELanguage language = AppEnum.ELanguage.En)
    {
        var member = CreateMemberFieldName(userId, nickname);
        var rankingKey = CreateRankingKeyName(rankingType);
        
        var rank = 
            await mRedisClient.GetMemberRank(rankingKey, member);

        var score = 
            await mRedisClient.GetMemberScore(rankingKey, member);

        if (rank == null || score == null)
            return ApiResponse<RankRes>
                .From(AppEnum.EResponseStatus.SuccessEmptyRanking, language);

        var result = new RankRes
        {
            Rankings = [
                new RankInfo
                {
                    Rank = (int)rank + 1,
                    Nickname = nickname,
                    Score = (double)score
                }
            ]
        };

        return ApiResponse<RankRes>
            .From(AppEnum.EResponseStatus.Success, language, result);
    }

    public async Task<ApiResponse> AddScore(
        AppEnum.ERankingType rankingType, 
        string userId, string nickname, double score, 
        AppEnum.ELanguage language = AppEnum.ELanguage.En)
    {
        if (score <= 0)
            return ApiResponse
                .From(AppEnum.EResponseStatus.ScoreCannotBeNegative, language);
        
        var member = CreateMemberFieldName(userId, nickname);
        
        await mRedisClient.AddSortedSetAsync(CreateRankingKeyName(rankingType), member, score); 
        
        return ApiResponse
            .From(AppEnum.EResponseStatus.Success, language);
    }

    private static string CreateMemberFieldName(string userId, string nickname)
    {
        return $"{userId}/{nickname}";
    }

    private static string CreateRankingKeyName(AppEnum.ERankingType rankingType)
    {
        return $"{AppConstant.RANKING_ROOT}:{rankingType}";
    }
}