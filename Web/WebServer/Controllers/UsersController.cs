using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Common.Base;
using VerdantValorShared.Common.Web;
using VerdantValorShared.DTOs.Web;
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
        if (!Enum.TryParse<AppEnum.ELanguage>(request.Language, out var language))
            language = AppEnum.ELanguage.En;

        if (!Enum.TryParse<AppEnum.EAuthType>(request.AuthType, out var authType))
            return ApiResponse
                .From(AppEnum.EResponseStatus.InvalidInput, language);

        switch (authType)
        {
            case AppEnum.EAuthType.Join:
            {
                return await mUsersService.JoinAsync(
                    request.Email, request.Pw, request.Nickname, language);
            }
            case AppEnum.EAuthType.Login:
            {
                return await mUsersService.LoginAsync(
                    request.Email, request.Pw, language);
            }
        }
        
        return ApiResponse
            .From(AppEnum.EResponseStatus.Success, language);
    }
}