using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Common;
using SharedLibrary.Protocol.Common;
using SharedLibrary.Protocol.DTOs;
using WebServer.Common;
using WebServer.Services;

namespace WebServer.Controllers;

[Route($"{AppConstant.WEB_SERVER_API_BASE}/[controller]")]
[ApiController]
public class UsersApiIntegration : Controller
{
    private readonly UsersService mUsersService;

    public UsersApiIntegration(UsersService usersService)
    {
        mUsersService = usersService;
    }

    [HttpPost("Auth")]
    public async Task<ApiResponse> Auth([FromBody] AuthReq request)
    {
        if (!Enum.TryParse<AppEnum.ELanguage>(request.Language, out var language))
            language = AppEnum.ELanguage.En;

        if (!Enum.TryParse<AppEnum.EAuthType>(request.AuthType, out var authType))
        {
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidInput, language));
        }

        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Pw))
        {
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyRequiredField, language));
        }

        switch (authType)
        {
            case AppEnum.EAuthType.Join:
            {
                if (string.IsNullOrWhiteSpace(request.Nickname))
                {
                    return new ApiResponse(
                        ResponseStatus.FromResponseStatus(
                            EResponseStatus.EmptyRequiredField, language));
                }

                return await mUsersService.Join(
                    request.Email, request.Pw, request.Nickname, language);
            }
            case AppEnum.EAuthType.Login:
            {
                return await mUsersService.CheckPassword(
                    request.Email, request.Pw, language);
            }
            default:
            {
                return new ApiResponse(ResponseStatus.FromResponseStatus(
                    EResponseStatus.Success, language));
            }
        }
    }
}