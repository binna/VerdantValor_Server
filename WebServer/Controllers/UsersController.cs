using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Common;
using SharedLibrary.Helpers;
using SharedLibrary.Protocol.Common;
using SharedLibrary.Protocol.DTOs;
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
#if DEVELOPMENT
    public async Task<ApiResponse> Auth([FromBody] AuthReq request)
    {
#elif LIVE
    public async Task<ApiResponse> Auth([FromBody] EncryptReq encryptReq)
    {
        var request = SecurityHelper.DecryptRequest<AuthReq>(encryptReq);
        if (request == null)
            return new ApiResponse<RankRes>(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.FailDecrypt, AppEnum.ELanguage.Ko)); 
#endif
        if (!Enum.TryParse<AppEnum.ELanguage>(request.Language, out var language))
            language = AppEnum.ELanguage.En;

        if (!Enum.TryParse<AppEnum.EAuthType>(request.AuthType, out var authType))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidInput, language));

        switch (authType)
        {
            case AppEnum.EAuthType.Join:
                return await mUsersService.JoinAsync(
                    request.Email, request.Pw, request.Nickname, language);
            case AppEnum.EAuthType.Login:
                return await mUsersService.LoginAsync(
                    request.Email, request.Pw, language);
            default:
                return new ApiResponse(ResponseStatus.FromResponseStatus(
                    EResponseStatus.Success, language));
        }
    }
}