using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebServer.Common;
using WebServer.Contexts;
using WebServer.DTOs;
using WebServer.Services;

namespace WebServer.Controllers
{
    [Route(AppConstant.Route.API_BASE)]
    [ApiController]
    public class RankingController : Controller
    {
        private readonly RankingService rankingService;

        public RankingController(RankingService rankingService)
        {
            this.rankingService = rankingService;
        }

        [HttpPost("{type}/top")]
        public async Task<ApiResponse<List<RankInfo>>> GetTopRanking(string type, int limit)
        {
            AppConstant.RankingType rankingType;

            try
            {
                rankingType = Enum.Parse<AppConstant.RankingType>(type);
            }
            catch (Exception)
            {
                return new ApiResponse<List<RankInfo>>(ResponseStatus.invalidRankingType);
            }

            if (limit < AppConstant.RANKING_MIN || limit > AppConstant.RANKING_MAX)
            {
                return new ApiResponse<List<RankInfo>>(ResponseStatus.invalidRankingRange);
            }

            return await rankingService.GetTopRankingAsync(rankingType, limit);
        }

        [Authorize]
        [HttpPost("{type}/me")]
        public async Task<ApiResponse<RankRes>> GetRank(string type)
        {
            string? userId = User.FindFirst("userId")?.Value;
            string? nickname = User.FindFirst("nickname")?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(nickname))
                return new ApiResponse<RankRes>(ResponseStatus.invalidAuthToken);

            AppConstant.RankingType rankingType;

            try
            {
                rankingType = Enum.Parse<AppConstant.RankingType>(type);
            }
            catch (Exception)
            {
                return new ApiResponse<RankRes>(ResponseStatus.invalidRankingType);
            }

            return await rankingService.GetRankAsync(
                rankingType, 
                rankingService.CreateMemberFieldName(userId, nickname));
        }

        [Authorize]
        [HttpPost("{type}/entries")]
        public async Task<ApiResponse> CreateScore(string type, [FromBody] CreateScoreReq request)
        {
            string? userId = User.FindFirst("userId")?.Value;
            string? nickname = User.FindFirst("nickname")?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(nickname))
                return new ApiResponse(ResponseStatus.invalidAuthToken);

            AppConstant.RankingType rankingType;

            try
            {
                rankingType = Enum.Parse<AppConstant.RankingType>(type);
            }
            catch (Exception)
            {
                return new ApiResponse<RankRes>(ResponseStatus.invalidRankingType);
            }

            return await rankingService.AddScore(
                rankingType, 
                rankingService.CreateMemberFieldName(userId, nickname),
                request.score);
        }
    }
}
