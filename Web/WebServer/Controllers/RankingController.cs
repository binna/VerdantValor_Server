using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Common.Web;
using Protocol.Web.Dtos;
using Shared.Constants;
using Shared.Types;
using WebServer.Services;

namespace WebServer.Controllers;

[Route($"{AppConstant.WEB_SERVER_API_BASE}/[controller]")]
[ApiController]
public class RankingController : Controller
{
    private readonly ILogger<RankingController> mLogger;
    private readonly RankingService mRankingService;
    
    public RankingController(
        ILogger<RankingController> logger, 
        RankingService rankingService)
    {
        mLogger = logger;
        mRankingService = rankingService;
    }

    [HttpPost("GetRank")]
    [Authorize(Policy = "SessionPolicy")]
    public async Task<ApiResponse<RankRes>> GetRank([FromBody] GetRankReq request)
    {
        var userId = this.GetUserId();
        
        if (!Enum.TryParse<ERankingScope>(request.Scope, out var rankingScope) 
                || !Enum.TryParse<ERanking>(request.Type, out var rankingType))
            return ApiResponse<RankRes>
                .From(EResponseResult.InvalidInput);
        
        switch (rankingScope)
        {
            case ERankingScope.Global:
            {
                var result = await mRankingService
                    .GetTopRankingAsync(rankingType, request.Limit);
                return ApiResponse<RankRes>.From(result.Item1, result.Item2);
            }
            case ERankingScope.My:
            {
                var result = await mRankingService
                    .GetMemberRankAsync(rankingType, userId, this.GetNickname());
                return ApiResponse<RankRes>.From(result.Item1, result.Item2);
            }
            default:
                mLogger.LogError("Invalid Scope {Scope}", rankingScope);
                return ApiResponse<RankRes>.From(EResponseResult.InvalidInput);
        }
    }

    [HttpPost("Entries")]
    [Authorize(Policy = "SessionPolicy")]
    public async Task<ApiResponse> Entries([FromBody] CreateScoreReq request)
    {
        var userId = this.GetUserId();
        var nickname = this.GetNickname();

        if (!Enum.TryParse<ERanking>(request.Type, out var rankingType))
            return ApiResponse<RankRes>.From(EResponseResult.InvalidInput);

        var code =  await mRankingService.AddScore(rankingType, userId, nickname, request.Score);
        return ApiResponse.From(code);
    }
}
