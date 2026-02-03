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
public class UsersController : Controller
{
    private readonly UsersService mUsersService;

    public UsersController(UsersService usersService)
    {
        mUsersService = usersService;
    }

    [HttpPost("Auth")]
    [AllowAnonymous]
    public async Task<ApiResponse> Auth([FromBody] AuthReq request)
    {
        if (!Enum.TryParse<ELanguage>(request.Language, out var language))
            language = ELanguage.En;

        if (!Enum.TryParse<EAuth>(request.AuthType, out var authType))
            return ApiResponse
                .From(EResponseResult.InvalidInput, language);

        switch (authType)
        {
            case EAuth.Join:
            {
                return await mUsersService.JoinAsync(
                    request.Email, request.Pw, request.Nickname, language);
            }
            case EAuth.Login:
            {
                return await mUsersService.LoginAsync(
                    request.Email, request.Pw, language);
            }
        }
        
        return ApiResponse
            .From(EResponseResult.Success, language);
    }
}