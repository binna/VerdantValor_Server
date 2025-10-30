using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs;
using WebServer.Common;
using WebServer.Services;

namespace WebServer.Controllers;

[Route(AppConstant.Route.API_BASE)]
[ApiController]
public class RankingController : Controller
{
    private readonly ILogger<RankingController> mLogger;
    private readonly RankingService mRankingService;
    private readonly HttpContext mHttpContext;
    
    public RankingController(
        ILogger<RankingController> logger,
        RankingService rankingService, 
        IHttpContextAccessor httpContextAccessor)
    {
        mLogger = logger;
        mRankingService = rankingService;

        if (httpContextAccessor.HttpContext == null)
        {
            mLogger.LogCritical("HttpContext is missing required configuration for session-based authentication.");
            Environment.Exit(1);
        }
        
        mHttpContext = httpContextAccessor.HttpContext;
    }

    [HttpPost("{type}/GetTopRanking")]
    public async Task<ApiResponse<List<RankInfo>>> GetTopRanking(string type, int limit)
    {
        var userId = mHttpContext.Session.GetString("userId");
        var nickname = mHttpContext.Session.GetString("nickname");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(nickname))
            return new ApiResponse<List<RankInfo>>(ResponseStatus.invalidAuthToken);
        
        AppConstant.RankingType rankingType;

        try
        {
            rankingType = Enum.Parse<AppConstant.RankingType>(type);
        }
        catch (Exception)
        {
            return new ApiResponse<List<RankInfo>>(ResponseStatus.invalidRankingType);
        }

        return await mRankingService.GetTopRankingAsync(rankingType, limit);
    }

    [HttpPost("{type}/GetRank")]
    public async Task<ApiResponse<RankRes>> GetRank(string type)
    {
        var userId = mHttpContext.Session.GetString("userId");
        var nickname = mHttpContext.Session.GetString("nickname");

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

        return await mRankingService.GetRankAsync(
            rankingType, 
            mRankingService.CreateMemberFieldName(userId, nickname));
    }

    [HttpPost("{type}/Entries")]
    public async Task<ApiResponse> Entries(string type, [FromBody] CreateScoreReq request)
    {
        var userId = mHttpContext.Session.GetString("userId");
        var nickname = mHttpContext.Session.GetString("nickname");

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

        return await mRankingService.AddScore(
            rankingType, 
            mRankingService.CreateMemberFieldName(userId, nickname),
            request.Score);
    }
}