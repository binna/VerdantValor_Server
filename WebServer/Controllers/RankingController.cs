using Microsoft.AspNetCore.Authorization;
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

    [Authorize(Policy = "AtLeast21")]
    [HttpPost("{type}/GetTopRanking")]
    public async Task<ApiResponse<List<RankInfo>>> GetTopRanking(string type, int limit)
    {
        var httpContext = mHttpContextAccessor.HttpContext;
        if (httpContext == null)
            return new ApiResponse<List<RankInfo>>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyAuth, AppConstant.ELanguage.En));
        
        

        var userId = httpContext.Session.GetString("userId");
        var languageCode = httpContext.Session.GetString("language");

        Console.WriteLine(userId + ", " + languageCode);
            
        if (!Enum.TryParse<AppConstant.ELanguage>(languageCode, out var language))
            language = AppConstant.ELanguage.En;
        
        if (userId == null)
            return new ApiResponse<List<RankInfo>>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidAuth, language));

        var sessionId = (await RedisClient.Instance.GetSessionInfoAsync(userId)).ToString();
        Console.WriteLine("select > " + sessionId);
        Console.WriteLine("now > " + httpContext.Session.Id);
        
        if (!sessionId.Equals(httpContext.Session.Id))
            return new ApiResponse<List<RankInfo>>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidAuth, language));
        
        if (!Enum.TryParse<AppConstant.ERankingType>(type, out var rankingType))
            return new ApiResponse<List<RankInfo>>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidRankingType, language));

        return await mRankingService.GetTopRankingAsync(rankingType, limit, language);
    }

    [HttpPost("{type}/GetRank")]
    public async Task<ApiResponse<RankRes>> GetRank(string type)
    {
        var httpContext = mHttpContextAccessor.HttpContext;
        if (httpContext == null)
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyAuth, AppConstant.ELanguage.En));
        
        var userId = httpContext.Session.GetString("userId");
        var nickname = httpContext.Session.GetString("nickname");
        var languageCode = httpContext.Session.GetString("language");
        
        if (!Enum.TryParse<AppConstant.ELanguage>(languageCode, out var language))
            language = AppConstant.ELanguage.En;

        Console.WriteLine(userId + ", " + nickname);
        
        
        if (userId == null || nickname == null)
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidAuth, language));
        
        var sessionId = (await RedisClient.Instance.GetSessionInfoAsync(userId)).ToString();
        
        if (!sessionId.Equals(httpContext.Session.Id))
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidRankingType, language));
        
        if (!Enum.TryParse<AppConstant.ERankingType>(type, out var rankingType))
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidRankingType, language));

        return await mRankingService.GetRankAsync(
            rankingType, 
            RankingService.CreateMemberFieldName(userId, nickname), 
            language);
    }

    [HttpPost("{type}/Entries")]
    public async Task<ApiResponse> Entries(string type, [FromBody] CreateScoreReq request)
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

        if (!Enum.TryParse<AppConstant.ERankingType>(type, out var rankingType))
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