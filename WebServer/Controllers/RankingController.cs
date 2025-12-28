using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Common;
using SharedLibrary.Protocol.Common.Web;
using SharedLibrary.Protocol.DTOs.Web;
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
        
        if (!Enum.TryParse<AppEnum.ERankingScope>(request.Scope, out var rankingScope) 
                || !Enum.TryParse<AppEnum.ERankingType>(request.Type, out var rankingType))
            return ApiResponse<RankRes>
                .From(AppEnum.EResponseStatus.InvalidInput, language);
        
        switch (rankingScope)
        {
            case AppEnum.ERankingScope.My:
            {
                var nickname = this.GetNickname();
                return await mRankingService.GetMemberRankAsync(
                    rankingType, userId, nickname, language);
            }
            case AppEnum.ERankingScope.Global:
            {
                return await mRankingService.GetTopRankingAsync(
                    rankingType, request.Limit, language);
            }
        }
        return ApiResponse<RankRes>
            .From(AppEnum.EResponseStatus.Success, language);
    }

    [HttpPost("Entries")]
    [Authorize(Policy = "SessionPolicy")]
    public async Task<ApiResponse> Entries([FromBody] CreateScoreReq request)
    {
        var userId = this.GetUserId();
        var language = this.GetLanguage();
        var nickname = this.GetNickname();

        if (!Enum.TryParse<AppEnum.ERankingType>(request.Type, out var rankingType))
            return ApiResponse<RankRes>
                .From(AppEnum.EResponseStatus.InvalidInput, language);

        return await mRankingService.AddScore(
            rankingType, userId, nickname, request.Score, language);
    }
}