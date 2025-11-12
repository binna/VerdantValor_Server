using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
#if DEVELOPMENT
    public async Task<ApiResponse<RankRes>> GetRank([FromBody] GetRankReq request)
    {
#elif LIVE
    public async Task<ApiResponse<RankRes>> GetRank([FromBody] EncryptReq encryptReq)
    {
        GetRankReq? request;
        using (var aesCcm = new AesCcm(AppReadonly.REQ_ENCRYPT_KEY))
        {
            var nonceBytes = Convert.FromBase64String(encryptReq.Nonce);
            var dataBytes = Convert.FromBase64String(encryptReq.Data);
            var tagBytes = Convert.FromBase64String(encryptReq.Tag);
            var plainBytes = new byte[nonceBytes.Length];
        
            aesCcm.Decrypt(nonceBytes, dataBytes, tagBytes, plainBytes);

            var plaintext = Encoding.UTF8.GetString(plainBytes);
        
            request = JsonSerializer.Deserialize<GetRankReq>(plaintext);
            
            if (request == null)
                return new ApiResponse<RankRes>(
                    ResponseStatus.FromResponseStatus(
                        EResponseStatus.FailDecrypt, AppEnum.ELanguage.Ko)); 
        }
#endif
        var httpContext = mHttpContextAccessor.HttpContext;
        if (httpContext == null)
            throw new InvalidOperationException(ExceptionMessage.EMPTY_HTTP_CONTEXT);
        
        var userId = httpContext.Session.GetString("userId");
        var languageCode = httpContext.Session.GetString("language");
        
        if (!Enum.TryParse<AppEnum.ELanguage>(languageCode, out var language))
            language = AppEnum.ELanguage.En;

        if (userId == null)
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidAuth, language));

        var sessionId = (await mRedisClient.GetSessionInfoAsync(userId)).ToString();
        if (!sessionId.Equals(httpContext.Session.Id))
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidAuth, language));

        if (!Enum.TryParse<AppEnum.ERankingScope>(request.Scope, out var rankingScope) 
                || !Enum.TryParse<AppEnum.ERankingType>(request.Type, out var rankingType))
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidInput, language));

        switch (rankingScope)
        {
            case AppEnum.ERankingScope.My:
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
            case AppEnum.ERankingScope.Global:
                return await mRankingService.GetTopRankingAsync(
                    rankingType, request.Limit, language);
            default:
                return new ApiResponse<RankRes>(
                    ResponseStatus.FromResponseStatus(
                        EResponseStatus.Success, language));
        }
    }

    [HttpPost("Entries")]
#if DEVELOPMENT
    public async Task<ApiResponse> Entries([FromBody] CreateScoreReq request)
    {
#elif LIVE
    public async Task<ApiResponse> Entries([FromBody] EncryptReq encryptReq)
    {
        CreateScoreReq? request;
        using (var aesCcm = new AesCcm(AppReadonly.REQ_ENCRYPT_KEY))
        {
            var nonceBytes = Convert.FromBase64String(encryptReq.Nonce);
            var dataBytes = Convert.FromBase64String(encryptReq.Data);
            var tagBytes = Convert.FromBase64String(encryptReq.Tag);
            var plainBytes = new byte[nonceBytes.Length];
        
            aesCcm.Decrypt(nonceBytes, dataBytes, tagBytes, plainBytes);

            var plaintext = Encoding.UTF8.GetString(plainBytes);
        
            request = JsonSerializer.Deserialize<CreateScoreReq>(plaintext);
            
            if (request == null)
                return new ApiResponse<RankRes>(
                    ResponseStatus.FromResponseStatus(
                        EResponseStatus.FailDecrypt, AppEnum.ELanguage.Ko)); 
        }
#endif
        var httpContext = mHttpContextAccessor.HttpContext;
        if (httpContext == null)
            throw new InvalidOperationException(ExceptionMessage.EMPTY_HTTP_CONTEXT);

        var userId = httpContext.Session.GetString("userId");
        var nickname = httpContext.Session.GetString("nickname");
        var languageCode = httpContext.Session.GetString("language");
        
        if (!Enum.TryParse<AppEnum.ELanguage>(languageCode, out var language))
            language = AppEnum.ELanguage.En;

        if (string.IsNullOrWhiteSpace(userId) 
                || string.IsNullOrWhiteSpace(nickname))
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidAuth, language));

        var sessionId = (await mRedisClient.GetSessionInfoAsync(userId)).ToString();
        if (!sessionId.Equals(httpContext.Session.Id))
        {
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidAuth, language));
        }

        if (!Enum.TryParse<AppEnum.ERankingType>(request.Type, out var rankingType))
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidInput, language));

        return await mRankingService.AddScore(
            rankingType, userId, nickname, request.Score, language);
    }
}