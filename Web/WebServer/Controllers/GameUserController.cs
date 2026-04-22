using Common.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Protocol.Web.Dtos;
using Shared.Constants;
using Shared.Types;
using WebServer.Services;

namespace WebServer.Controllers;

[Route($"{AppConstant.WEB_SERVER_API_BASE}/[controller]")]
[ApiController]
public class GameUserController : Controller
{
    readonly ILogger<GameUserController> mLogger;
    private readonly GameUserService mGameUserService;

    public GameUserController(
        ILogger<GameUserController> logger,
        GameUserService gameUserService)
    {
        mLogger = logger;
        mGameUserService = gameUserService;
    }

    [HttpPost("Auth")]
    [AllowAnonymous]
    public async Task<ApiResponse> Auth([FromBody] AuthReq request)
    {
        if (!Enum.TryParse<EAuth>(request.AuthType, out var authType))
            return ApiResponse.From(EResponseResult.InvalidInput);

        EResponseResult responseResult;

        switch (authType)
        {
            case EAuth.Join:
                responseResult = await mGameUserService.JoinAsync(request.Email, request.Pw, request.Nickname);
                break;
            case EAuth.Login:
                responseResult = await mGameUserService.LoginAsync(request.Email, request.Pw);
                break;
            default:
                responseResult = EResponseResult.InvalidAuthType;
                mLogger.LogError("Invalid authType {AuthType}", authType);
                break;
        }
        
        return ApiResponse.From(responseResult);
    }
}