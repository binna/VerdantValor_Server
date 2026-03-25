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
    private readonly GameUserService mGameUserService;

    public GameUserController(GameUserService gameUserService)
    {
        mGameUserService = gameUserService;
    }

    [HttpPost("Auth")]
    [AllowAnonymous]
    public async Task<ApiResponse> Auth([FromBody] AuthReq request)
    {
        if (!Enum.TryParse<EAuth>(request.AuthType, out var authType))
            return ApiResponse.From(EResponseResult.InvalidInput);

        switch (authType)
        {
            case EAuth.Join:
                return await mGameUserService.JoinAsync(
                    request.Email, request.Pw, request.Nickname);
            case EAuth.Login:
                return await mGameUserService.LoginAsync(
                    request.Email, request.Pw);
        }
        
        return ApiResponse.From(EResponseResult.Success);
    }
}