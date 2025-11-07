using SharedLibrary.Common;
using SharedLibrary.Protocol.Common;
using SharedLibrary.Protocol.DTOs;
using SharedLibrary.Redis;
using WebServer.Common;

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

    public async Task<ApiResponse<RankRes>> GetTopRankingAsync(AppEnum.ERankingType rankingType, int limit, AppEnum.ELanguage language)
    {
        if (limit is < AppConstant.RANKING_MIN or > AppConstant.RANKING_MAX)
        {
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidInput, language));
        }

        var redisRankings =
            await mRedisClient.GetTopRankingByType(CreateRankingKeyName(rankingType), limit);

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

    public async Task<ApiResponse<RankRes>> GetMemberRankAsync(AppEnum.ERankingType rankingType, string userId, string nickname, AppEnum.ELanguage language)
    {
        var member = CreateMemberFieldName(userId, nickname);
        
        var rank = 
            await mRedisClient.GetMemberRank(CreateRankingKeyName(rankingType), member);

        var score = 
            await mRedisClient.GetMemberScore(CreateRankingKeyName(rankingType), member);

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

    public async Task<ApiResponse> AddScore(AppEnum.ERankingType eRankingType, string userId, string nickname, double score, AppEnum.ELanguage language)
    {
        var member = CreateMemberFieldName(userId, nickname);
        
        await mRedisClient.AddSortedSetAsync(CreateRankingKeyName(eRankingType), member, score); 
        
        return new ApiResponse(
            ResponseStatus.FromResponseStatus(
                EResponseStatus.Success, language));
    }

    private static string CreateMemberFieldName(string userId, string nickname)
    {
        return $"{userId}/{nickname}";
    }

    private static string CreateRankingKeyName(AppEnum.ERankingType eRankingType)
    {
        return $"{AppConstant.RANKING_ROOT}:{eRankingType}";
    }
}