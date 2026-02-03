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
    private readonly RankingService mRankingService;
    
    public RankingController(RankingService rankingService)
    {
        mRankingService = rankingService;
    }

    [HttpPost("GetRank")]
    [Authorize(Policy = "SessionPolicy")]
    public async Task<ApiResponse<RankRes>> GetRank([FromBody] GetRankReq request)
    {
        var userId = this.GetUserId();
        var language = this.GetLanguage();
        
        if (!Enum.TryParse<ERankingScope>(request.Scope, out var rankingScope) 
                || !Enum.TryParse<ERanking>(request.Type, out var rankingType))
            return ApiResponse<RankRes>
                .From(EResponseResult.InvalidInput, language);
        
        switch (rankingScope)
        {
            case ERankingScope.My:
            {
                var nickname = this.GetNickname();
                return await mRankingService.GetMemberRankAsync(
                    rankingType, userId, nickname, language);
            }
            case ERankingScope.Global:
            {
                return await mRankingService.GetTopRankingAsync(
                    rankingType, request.Limit, language);
            }
        }
        return ApiResponse<RankRes>
            .From(EResponseResult.Success, language);
    }

    [HttpPost("Entries")]
    [Authorize(Policy = "SessionPolicy")]
    public async Task<ApiResponse> Entries([FromBody] CreateScoreReq request)
    {
        var userId = this.GetUserId();
        var language = this.GetLanguage();
        var nickname = this.GetNickname();

        if (!Enum.TryParse<ERanking>(request.Type, out var rankingType))
            return ApiResponse<RankRes>
                .From(EResponseResult.InvalidInput, language);

        return await mRankingService.AddScore(
            rankingType, userId, nickname, request.Score, language);
    }
}