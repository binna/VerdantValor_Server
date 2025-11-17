using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Common;
using SharedLibrary.Protocol.Common;
using SharedLibrary.Protocol.DTOs;
using SharedLibrary.Redis;
using WebServer.Services;

namespace WebServer.Controllers;

[Route($"{AppConstant.WEB_SERVER_API_BASE}/[controller]")]
[ApiController]
public class RankingController : Controller
{
    private readonly RankingService mRankingService;
    private readonly IHttpContextAccessor mHttpContextAccessor;
    private readonly IRedisClient mRedisClient;
    
    public RankingController(
        RankingService rankingService,
        IHttpContextAccessor httpContextAccessor,
        IRedisClient redisClient)
    {
        mRankingService = rankingService;
        mHttpContextAccessor = httpContextAccessor;
        mRedisClient = redisClient;
    }

    [HttpPost("GetRank")]
    [Authorize(Policy = "SessionPolicy")]
    public async Task<ApiResponse<RankRes>> GetRank([FromBody] GetRankReq request)
    {
        var userId = (string)mHttpContextAccessor.HttpContext!.Items["userId"]!;
        var language = (AppEnum.ELanguage)mHttpContextAccessor.HttpContext!.Items["language"]!;
        
        if (!Enum.TryParse<AppEnum.ERankingScope>(request.Scope, out var rankingScope) 
                || !Enum.TryParse<AppEnum.ERankingType>(request.Type, out var rankingType))
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidInput, language));
        
        switch (rankingScope)
        {
            case AppEnum.ERankingScope.My:
            {
                var nickname = (string)mHttpContextAccessor.HttpContext!.Items["nickname"]!;
                return await mRankingService.GetMemberRankAsync(
                    rankingType, userId, nickname, language);
            }
            case AppEnum.ERankingScope.Global:
            {
                return await mRankingService.GetTopRankingAsync(
                    rankingType, request.Limit, language);
            }
        }
        return new ApiResponse<RankRes>(
            ResponseStatus.FromResponseStatus(
                EResponseStatus.Success, language));
    }

    [HttpPost("Entries")]
    [Authorize(Policy = "SessionPolicy")]
    public async Task<ApiResponse> Entries([FromBody] CreateScoreReq request)
    {
        var userId = (string)mHttpContextAccessor.HttpContext!.Items["userId"]!;
        var language = (AppEnum.ELanguage)mHttpContextAccessor.HttpContext!.Items["language"]!;
        var nickname = (string)mHttpContextAccessor.HttpContext!.Items["nickname"]!;

        if (!Enum.TryParse<AppEnum.ERankingType>(request.Type, out var rankingType))
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidInput, language));

        return await mRankingService.AddScore(
            rankingType, userId, nickname, request.Score, language);
    }
}