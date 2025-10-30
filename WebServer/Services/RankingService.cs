using SharedLibrary.DTOs;
using WebServer.Common;
using WebServer.Infrastructure;

namespace WebServer.Services;

public class RankingService
{
    private readonly ILogger<RankingService> mLogger;
    private readonly RedisClient mRedisClient;
    private readonly ApplicationDbContext mDbContext;

    public RankingService(
        ILogger<RankingService> logger, 
        RedisClient redisClient, ApplicationDbContext dbContext)
    {
        mLogger = logger;
        mRedisClient = redisClient;
        mDbContext = dbContext;
    }

    public async Task<ApiResponse<List<RankInfo>>> GetTopRankingAsync(AppConstant.RankingType rankingType, int limit)
    {
        if (limit is < AppConstant.RANKING_MIN or > AppConstant.RANKING_MAX)
            return new ApiResponse<List<RankInfo>>(ResponseStatus.invalidRankingRange);
        
        try
        {
            var redisRankings =
                await mRedisClient.GetTopRankingByType(CreateRankingKeyName(rankingType), limit);

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

            return new ApiResponse<List<RankInfo>>(ResponseStatus.success, rankingList);
        }
        catch (Exception ex)
        {
            mLogger.LogError($"[Error] {ex.StackTrace}");
            return new ApiResponse<List<RankInfo>>(ResponseStatus.redisError);
        }
    }

    public async Task<ApiResponse<RankRes>> GetRankAsync(AppConstant.RankingType rankingType, string member)
    {
        var rank = await mRedisClient
            .GetMemberRank(CreateRankingKeyName(rankingType), member);

        var score = await mRedisClient
            .GetMemberScore(CreateRankingKeyName(rankingType), member);

        if (rank == null || score == null)
            return new ApiResponse<RankRes>(ResponseStatus.successEmptyRanking);

        var rankInfo = new RankRes()
        {
            Rank = (long)rank + 1,
            Score = (double)score
        };

        return new ApiResponse<RankRes>(ResponseStatus.success, rankInfo);
    }

    public async Task<ApiResponse> AddScore(AppConstant.RankingType rankingType, string member, double score)
    {
        var result = await mRedisClient
            .AddSortedSetAsync(CreateRankingKeyName(rankingType), member, score);

        if (result)
            return new ApiResponse(ResponseStatus.success);

        return new ApiResponse<RankRes>(ResponseStatus.redisError);
    }

    public string CreateMemberFieldName(string userId, string nickname)
    {
        return $"{userId}/{nickname}";
    }

    private string CreateRankingKeyName(AppConstant.RankingType rankingType)
    {
        return $"{AppConstant.RANKING_ROOT}:{rankingType}";
    }
}