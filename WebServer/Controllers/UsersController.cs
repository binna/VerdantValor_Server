using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Common;
using SharedLibrary.DTOs;
using WebServer.Common;
using WebServer.Services;

namespace WebServer.Controllers;

[Route(AppConstant.WEB_SERVER_API_BASE)]
[ApiController]
public class UsersController : Controller
{
    private readonly UsersService mUsersService;

    public UsersController(UsersService usersService)
    {
        mUsersService = usersService;
    }

    [HttpPost("Auth")]
    public async Task<ApiResponse> Auth([FromBody] AuthReq request)
    {
        if (!Enum.TryParse<AppConstant.ELanguage>(request.Language, out var language))
            language = AppConstant.ELanguage.En;

        if (!Enum.TryParse<AppConstant.EAuthType>(request.AuthType, out var authType))
        {
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyEmail, language));
        }
        
        if (string.IsNullOrWhiteSpace(request.Email))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyEmail, language));

        if (string.IsNullOrWhiteSpace(request.Pw))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyPw, language));

        switch (authType)
        {
            case AppConstant.EAuthType.Join:
                if (string.IsNullOrWhiteSpace(request.Nickname))
                    return new ApiResponse(
                        ResponseStatus.FromResponseStatus(
                            EResponseStatus.EmptyNickname, language));
                return await mUsersService.Join(request.Email, request.Pw, request.Nickname, language);
            case AppConstant.EAuthType.Login:
                return await mUsersService.CheckPassword(request.Email, request.Pw, language);
            default:
                return new ApiResponse(ResponseStatus.FromResponseStatus(
                    EResponseStatus.Success, language));
        }
    }
}