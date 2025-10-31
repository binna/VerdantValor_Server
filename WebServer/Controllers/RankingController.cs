using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs;
using WebServer.Common;
using WebServer.Services;

namespace WebServer.Controllers;

[Route(AppConstant.Route.API_BASE)]
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
            return new ApiResponse<List<RankInfo>>(ResponseStatus.emptyAuth);
        
        var userId = httpContext.Session.GetString("userId");
        var nickname = httpContext.Session.GetString("nickname");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(nickname))
            return new ApiResponse<List<RankInfo>>(ResponseStatus.invalidAuth);
        
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
        var httpContext = mHttpContextAccessor.HttpContext;
        if (httpContext == null)
            return new ApiResponse<RankRes>(ResponseStatus.emptyAuth);
        
        var userId = httpContext.Session.GetString("userId");
        var nickname = httpContext.Session.GetString("nickname");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(nickname))
            return new ApiResponse<RankRes>(ResponseStatus.invalidAuth);

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
            RankingService.CreateMemberFieldName(userId, nickname));
    }

    [HttpPost("{type}/Entries")]
    public async Task<ApiResponse> Entries(string type, [FromBody] CreateScoreReq request)
    {
        var httpContext = mHttpContextAccessor.HttpContext;
        if (httpContext == null)
            return new ApiResponse(ResponseStatus.emptyAuth);
        
        var userId = httpContext.Session.GetString("userId");
        var nickname = httpContext.Session.GetString("nickname");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(nickname))
            return new ApiResponse(ResponseStatus.invalidAuth);

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
            RankingService.CreateMemberFieldName(userId, nickname),
            request.Score);
    }
}