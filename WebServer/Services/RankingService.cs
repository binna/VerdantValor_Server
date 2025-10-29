using StackExchange.Redis;
using WebServer.Common;
using WebServer.Configs;
using WebServer.Contexts;
using WebServer.DTOs;

namespace WebServer.Services
{
    public class RankingService
    {
        private readonly ILogger<RankingService> logger;

        private readonly RedisClient redisClient;

        public RankingService(ILogger<RankingService> logger,
                              RedisClient redisClient)
        {
            this.logger = logger;

            this.redisClient = redisClient;
        }

        public async Task<ApiResponse<List<RankInfo>>> GetTopRankingAsync(AppConstant.RankingType rankingType, int rank)
        {
            try
            {
                SortedSetEntry[] redisRankings =
                    await redisClient.GetTopRankingByType(CreateRankingKeyName(rankingType), rank);

                List<RankInfo> rankingList = new(rank);

                int index = 0;

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
                        logger.LogError("Failed to parse userId.\nCheck Redis data.");
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
                logger.LogError($"[Error] {ex.StackTrace}");
                return new ApiResponse<List<RankInfo>>(ResponseStatus.redisError);
            }
        }

        public async Task<ApiResponse<RankRes>> GetRankAsync(AppConstant.RankingType rankingType, string member)
        {
            var rank = await redisClient
                .GetMemberRank(CreateRankingKeyName(rankingType), member);

            var score = await redisClient
                .GetMemberScore(CreateRankingKeyName(rankingType), member);

            if (rank == null && score == null)
                return new ApiResponse<RankRes>(ResponseStatus.successEmptyRanking);

            var rankInfo = new RankRes()
            {
                Rank = rank == null ? 0 : (long)rank + 1,
                Score = score == null ? 0 : (double)score
            };

            return new ApiResponse<RankRes>(ResponseStatus.success, rankInfo);
        }

        public async Task<ApiResponse> AddScore(AppConstant.RankingType rankingType, string member, double score)
        {
            var result = await redisClient
                .AddSortedSetAsync(CreateRankingKeyName(rankingType), member, score);

            if (result)
                return new ApiResponse(ResponseStatus.success);

            return new ApiResponse<RankRes>(ResponseStatus.redisError);
        }

        public string CreateMemberFieldName(string userId, string nickname)
        {
            return $"{userId}/{nickname}";
        }

        public string CreateRankingKeyName(AppConstant.RankingType rankingType)
        {
            return $"{AppConstant.RANKING_ROOT}:{rankingType}";
        }
    }
}
