using SharedLibrary.Common;
using SharedLibrary.Database.Redis;
using SharedLibrary.DTOs;
using WebServer.Common;

namespace WebServer.Services;

public class RankingService
{
    private readonly ILogger<RankingService> mLogger;

    public RankingService(
        ILogger<RankingService> logger)
    {
        mLogger = logger;
    }

    public async Task<ApiResponse<RankRes>> GetTopRankingAsync(AppConstant.ERankingType rankingType, int limit, AppConstant.ELanguage language)
    {
        if (limit is < AppConstant.RANKING_MIN or > AppConstant.RANKING_MAX)
        {
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidInput, language));
        }

        var redisRankings =
            await RedisClient.Instance.GetTopRankingByType(CreateRankingKeyName(rankingType), limit);

        List<RankInfo> rankingList = new(limit);

        for (var i = 0; i < limit; i++)
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

        return new ApiResponse<RankRes>(
            ResponseStatus.FromResponseStatus(
                EResponseStatus.Success, language),
            new RankRes { Rankings = rankingList });
    }

    public async Task<ApiResponse<RankRes>> GetMemberRankAsync(AppConstant.ERankingType rankingType, string userId, string nickname, AppConstant.ELanguage language)
    {
        var member = CreateMemberFieldName(userId, nickname);
        
        var rank = await RedisClient.Instance
            .GetMemberRank(CreateRankingKeyName(rankingType), member);

        var score = await RedisClient.Instance
            .GetMemberScore(CreateRankingKeyName(rankingType), member);

        if (rank == null || score == null)
        {
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.SuccessEmptyRanking, language));
        }

        var rankInfo = new RankRes
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

        return new ApiResponse<RankRes>(
            ResponseStatus.FromResponseStatus(
                EResponseStatus.Success, language), 
            rankInfo);
    }

    public async Task<ApiResponse> AddScore(AppConstant.ERankingType eRankingType, string userId, string nickname, double score, AppConstant.ELanguage language)
    {
        var member = CreateMemberFieldName(userId, nickname);
        
        await RedisClient.Instance
            .AddSortedSetAsync(CreateRankingKeyName(eRankingType), member, score); 
        
        return new ApiResponse(
            ResponseStatus.FromResponseStatus(
                EResponseStatus.Success, language));
    }

    private static string CreateMemberFieldName(string userId, string nickname)
    {
        return $"{userId}/{nickname}";
    }

    private static string CreateRankingKeyName(AppConstant.ERankingType eRankingType)
    {
        return $"{AppConstant.RANKING_ROOT}:{eRankingType}";
    }
}