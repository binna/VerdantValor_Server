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
            throw new InvalidOperationException(ExceptionMessage.EMPTY_HTTP_CONTEXT);
        
        var userId = httpContext.Session.GetString("userId");
        var languageCode = httpContext.Session.GetString("language");
        
        if (!Enum.TryParse<AppConstant.ELanguage>(languageCode, out var language))
            language = AppConstant.ELanguage.En;

        if (userId == null)
        {
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidAuth, language));
        }

        var sessionId = (await RedisClient.Instance.GetSessionInfoAsync(userId)).ToString();
        if (!sessionId.Equals(httpContext.Session.Id))
        {
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidAuth, language));
        }

        if (!Enum.TryParse<AppConstant.ERankingScope>(request.Scope, out var rankingScope) 
                || !Enum.TryParse<AppConstant.ERankingType>(request.Type, out var rankingType))
        {
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidInput, language));
        }

        switch (rankingScope)
        {
            case AppConstant.ERankingScope.My:
            {
                var nickname = httpContext.Session.GetString("nickname");
                if (nickname == null)
                {
                    return new ApiResponse<RankRes>(
                        ResponseStatus.FromResponseStatus(
                            EResponseStatus.InvalidAuth, language));
                }

                return await mRankingService.GetMemberRankAsync(
                    rankingType, userId, nickname, language);
            }
            case AppConstant.ERankingScope.Global:
            {
                return await mRankingService.GetTopRankingAsync(
                    rankingType, request.Limit, language);
            }
            default:
            {
                return new ApiResponse<RankRes>(
                    ResponseStatus.FromResponseStatus(
                        EResponseStatus.Success, language));
            }
        }
    }

    [HttpPost("Entries")]
    public async Task<ApiResponse> Entries([FromBody] CreateScoreReq request)
    {
        var httpContext = mHttpContextAccessor.HttpContext;
        if (httpContext == null)
            throw new InvalidOperationException(ExceptionMessage.EMPTY_HTTP_CONTEXT);

        var userId = httpContext.Session.GetString("userId");
        var nickname = httpContext.Session.GetString("nickname");
        var languageCode = httpContext.Session.GetString("language");
        
        if (!Enum.TryParse<AppConstant.ELanguage>(languageCode, out var language))
            language = AppConstant.ELanguage.En;

        if (string.IsNullOrWhiteSpace(userId) 
                || string.IsNullOrWhiteSpace(nickname))
        {
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidAuth, language));
        }

        var sessionId = (await RedisClient.Instance.GetSessionInfoAsync(userId)).ToString();
        if (!sessionId.Equals(httpContext.Session.Id))
        {
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidAuth, language));
        }

        if (!Enum.TryParse<AppConstant.ERankingType>(request.Type, out var rankingType))
        {
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidInput, language));
        }

        return await mRankingService.AddScore(
            rankingType, userId, nickname, request.Score, language);
    }
}