using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Common;
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

    [HttpPost("{type}/GetTopRanking")]
    public async Task<ApiResponse<List<RankInfo>>> GetTopRanking(string type, int limit)
    {
        var httpContext = mHttpContextAccessor.HttpContext;
        if (httpContext == null)
            return new ApiResponse<List<RankInfo>>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyAuth, AppConstant.ELanguage.En));

        AppConstant.ELanguage language;
        
        try
        {
            var languageCode = httpContext.Session.GetString("language");
            
            if (string.IsNullOrEmpty(languageCode))
                return new ApiResponse<List<RankInfo>>(
                    ResponseStatus.FromResponseStatus(
                        EResponseStatus.ExpireAuth, AppConstant.ELanguage.En));
            
            language = Enum.Parse<AppConstant.ELanguage>(languageCode);
        }
        catch (Exception)
        {
            language = AppConstant.ELanguage.En;
        }
        
        AppConstant.ERankingType rankingType;

        try
        {
            rankingType = Enum.Parse<AppConstant.ERankingType>(type);
        }
        catch (Exception)
        {
            return new ApiResponse<List<RankInfo>>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidRankingType, language));
        }

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

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(languageCode))
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.ExpireAuth, AppConstant.ELanguage.En));
        
        AppConstant.ELanguage language;
        
        try
        {
            language = Enum.Parse<AppConstant.ELanguage>(languageCode);
        }
        catch (Exception)
        {
            language = AppConstant.ELanguage.En;
        }

        AppConstant.ERankingType rankingType;

        try
        {
            rankingType = Enum.Parse<AppConstant.ERankingType>(type);
        }
        catch (Exception)
        {
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidRankingType, language));
        }

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

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(languageCode))
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.ExpireAuth, AppConstant.ELanguage.En));
        
        AppConstant.ELanguage language;
        
        try
        {
            language = Enum.Parse<AppConstant.ELanguage>(languageCode);
        }
        catch (Exception)
        {
            language = AppConstant.ELanguage.En;
        }

        AppConstant.ERankingType eRankingType;

        try
        {
            eRankingType = Enum.Parse<AppConstant.ERankingType>(type);
        }
        catch (Exception)
        {
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidRankingType, language));
        }

        return await mRankingService.AddScore(
            eRankingType, 
            RankingService.CreateMemberFieldName(userId, nickname),
            request.Score,
            language);
    }
}