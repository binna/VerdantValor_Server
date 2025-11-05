using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Common;
using SharedLibrary.Database.Redis;
using SharedLibrary.DTOs;
using WebServer.Common;
using WebServer.Services;

namespace WebServer.Controllers;

[Route(AppConstant.WEB_SERVER_API_BASE)]
[ApiController]
public class RankingController : Controller
{
    private readonly RankingService mRankingService;
    private readonly IHttpContextAccessor mHttpContextAccessor;
    
    public RankingController(
        RankingService rankingService,
        IHttpContextAccessor httpContextAccessor)
    {
        mRankingService = rankingService;
        mHttpContextAccessor = httpContextAccessor;
    }

    [HttpPost("GetRank")]
    public async Task<ApiResponse<RankRes>> GetRank([FromBody] GetRankReq request)
    {
        var httpContext = mHttpContextAccessor.HttpContext;
        if (httpContext == null)
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyAuth, AppConstant.ELanguage.En));
        
        var userId = httpContext.Session.GetString("userId");
        var languageCode = httpContext.Session.GetString("language");
        
        if (!Enum.TryParse<AppConstant.ELanguage>(languageCode, out var language))
            language = AppConstant.ELanguage.En;

        if (userId == null)
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidAuth, language));
        
        var sessionId = (await RedisClient.Instance.GetSessionInfoAsync(userId)).ToString();
        
        if (!sessionId.Equals(httpContext.Session.Id))
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidRankingType, language));
        
        if (!Enum.TryParse<AppConstant.ERankingScope>(request.Scope, out var ERankingScope))
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidRankingScope, language));
        
        if (!Enum.TryParse<AppConstant.ERankingType>(request.Type, out var rankingType))
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidRankingType, language));

        switch (ERankingScope)
        {
            case AppConstant.ERankingScope.My:
                var nickname = httpContext.Session.GetString("nickname");
                if (nickname == null)
                    return new ApiResponse<RankRes>(
                        ResponseStatus.FromResponseStatus(
                            EResponseStatus.InvalidAuth, language));
                
                return await mRankingService.GetMemberRankAsync(
                    rankingType, 
                    RankingService.CreateMemberFieldName(userId, nickname), 
                    language);
            case AppConstant.ERankingScope.Global:
                return await mRankingService.GetTopRankingAsync(
                    rankingType, 
                    request.Limit, 
                    language);
            default:
                return new ApiResponse<RankRes>(
                    ResponseStatus.FromResponseStatus(
                        EResponseStatus.Success, 
                        language));
        }
    }

    [HttpPost("Entries")]
    public async Task<ApiResponse> Entries([FromBody] CreateScoreReq request)
    {
        var httpContext = mHttpContextAccessor.HttpContext;
        if (httpContext == null)
            return new ApiResponse<List<RankInfo>>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyAuth, AppConstant.ELanguage.En));

        var userId = httpContext.Session.GetString("userId");
        var nickname = httpContext.Session.GetString("nickname");
        var languageCode = httpContext.Session.GetString("language");
        
        if (!Enum.TryParse<AppConstant.ELanguage>(languageCode, out var language))
            language = AppConstant.ELanguage.En;

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(nickname))
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidAuth, AppConstant.ELanguage.En));
        
        var sessionId = (await RedisClient.Instance.GetSessionInfoAsync(userId)).ToString();
        
        if (!sessionId.Equals(httpContext.Session.Id))
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidRankingType, language));

        if (!Enum.TryParse<AppConstant.ERankingType>(request.Type, out var rankingType))
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidRankingType, language));

        return await mRankingService.AddScore(
            rankingType, 
            RankingService.CreateMemberFieldName(userId, nickname),
            request.Score,
            language);
    }
}