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
    public async Task<ApiResponse<AuthRes>> Auth([FromBody] AuthReq request)
    {
        if (!Enum.TryParse<EAuth>(request.AuthType, out var authType))
            return ApiResponse<AuthRes>
                .From(EResponseResult.InvalidInput);

        switch (authType)
        {
            case EAuth.Join:
                var code = await mGameUserService
                    .JoinAsync(request.Email, request.Pw, request.Nickname);
                return ApiResponse<AuthRes>.From(code);
            case EAuth.Login:
                var result = await mGameUserService
                    .LoginAsync(request.Email, request.Pw, request.DeviceId);
                return ApiResponse<AuthRes>.From(result.Item1, result.Item2);
                break;
            case EAuth.Logout:
            // TODO 로그아웃
            default:
                mLogger.LogError("Invalid authType {AuthType}", authType);
                return ApiResponse<AuthRes>.From(EResponseResult.InvalidAuthType);
        }
    }
}