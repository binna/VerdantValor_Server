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

    public async Task<ApiResponse<List<RankInfo>>> GetTopRankingAsync(AppConstant.ERankingType eRankingType, int limit, AppConstant.ELanguage language)
    {
        if (limit is < AppConstant.RANKING_MIN or > AppConstant.RANKING_MAX)
            return new ApiResponse<List<RankInfo>>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidRankingRange, language));
        
        try
        {
            var redisRankings =
                await RedisClient.Instance.GetTopRankingByType(CreateRankingKeyName(eRankingType), limit);

            List<RankInfo> rankingList = new(limit);

            foreach (var info in redisRankings)
            {
                string[] userInfo = info.Element.ToString().Split("/");

                ulong userId = 0;

                try
                {
                    userId = ulong.Parse(userInfo[0]);
                }
                catch (Exception)
                {
                    mLogger.LogError("Failed to parse userId.\nCheck Redis data.");
                }
                
                rankingList.Add(new()
                {
                    UserId = userId,
                    Nickname = userInfo[1],
                    Score = info.Score
                });
            }

            return new ApiResponse<List<RankInfo>>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.Success, language), 
                rankingList);
        }
        catch (Exception ex)
        {
            mLogger.LogError($"[Error] {ex.StackTrace}");
            return new ApiResponse<List<RankInfo>>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.RedisError, language));
        }
    }

    public async Task<ApiResponse<RankRes>> GetRankAsync(AppConstant.ERankingType eRankingType, string member, AppConstant.ELanguage language)
    {
        var rank = await RedisClient.Instance
            .GetMemberRank(CreateRankingKeyName(eRankingType), member);

        var score = await RedisClient.Instance
            .GetMemberScore(CreateRankingKeyName(eRankingType), member);

        if (rank == null || score == null)
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.SuccessEmptyRanking, language));

        var rankInfo = new RankRes()
        {
            Rank = (long)rank + 1,
            Score = (double)score
        };

        return new ApiResponse<RankRes>(
            ResponseStatus.FromResponseStatus(
                EResponseStatus.Success, language), 
            rankInfo);
    }

    public async Task<ApiResponse> AddScore(AppConstant.ERankingType eRankingType, string member, double score, AppConstant.ELanguage language)
    {
        try
        {
            await RedisClient.Instance
                .AddSortedSetAsync(CreateRankingKeyName(eRankingType), member, score);
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.Success, language));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.RedisError, language));
        }
    }

    public static string CreateMemberFieldName(string userId, string nickname)
    {
        return $"{userId}/{nickname}";
    }

    private static string CreateRankingKeyName(AppConstant.ERankingType eRankingType)
    {
        return $"{AppConstant.RANKING_ROOT}:{eRankingType}";
    }
}